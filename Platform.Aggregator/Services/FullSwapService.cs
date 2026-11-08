using System.Diagnostics;
using Grpc.Core;
using Messages.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Sessions.Grpc;
using Swaps.Grpc;

namespace Platform.Aggregator.Services;

public sealed class FullSwapService(
    SwapsService.SwapsServiceClient swaps,
    SessionsService.SessionsServiceClient sessions,
    MessagesService.MessagesServiceClient messages,
    FullSwapCache cache,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FullSwapService> logger)
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(10);

    public async Task<FullSwapDto?> GetFullSwapAsync(int swapId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("AggregateFullSwap");
        activity?.SetTag("swap.id", swapId);

        // The response depends on the caller, so entries are per user; a request without a valid user is never cached.
        var callerId = GetCallerId();
        if (callerId is { } cachedFor && await cache.TryGetAsync(swapId, cachedFor, ct) is { } cached)
            return cached;

        var swap = await GetSwapAsync(swapId, ct);
        if (swap is null)
            return null;

        var participantId = ResolveParticipant(swap);
        activity?.SetTag("user.id", participantId);

        var warnings = new List<string>();
        var sessionsTask = SafeAsync("sessions-api",
            token => GetSessionsAsync(swapId, token), [], warnings, ct);
        var messagesTask = SafeAsync("messages-api",
            token => GetConversationAsync(swapId, token), ConversationMessagesDto.Empty, warnings, ct);
        await Task.WhenAll(sessionsTask, messagesTask);

        var swapSessions = KeepOwned(await sessionsTask, s => s.SwapId == swapId, "sessions", swapId, warnings);
        var conversation = await messagesTask;
        var swapMessages = conversation.Conversation?.SwapId == swapId
            ? KeepOwned(conversation.Messages, m => m.ConversationId == conversation.Conversation.Id, "messages", swapId, warnings)
            : KeepOwned(conversation.Messages, _ => false, "messages", swapId, warnings);

        logger.LogInformation("Aggregated swap {SwapId} for {UserId}: {SessionCount} sessions, {MessageCount} messages, {WarningCount} warnings",
            swapId, participantId, swapSessions.Count, swapMessages.Count, warnings.Count);

        var result = new FullSwapDto(swap, swapSessions, swapMessages, warnings);

        // A partial response must not outlive the outage that caused it.
        if (callerId is { } userId && warnings.Count == 0)
            await cache.SetAsync(swapId, userId, result, ct);

        return result;
    }

    private Guid? GetCallerId()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[UserIdForwardingHandler.HeaderName].FirstOrDefault();
        return Guid.TryParse(header, out var userId) && userId != Guid.Empty ? userId : null;
    }

    private async Task<SwapDto?> GetSwapAsync(int swapId, CancellationToken ct)
    {
        try
        {
            var reply = await swaps.GetSwapAsync(
                new GetSwapRequest { Id = swapId }, deadline: Deadline(), cancellationToken: ct).ResponseAsync;
            return reply.ToDto();
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.PermissionDenied)
        {
            // A non-participant gets the same 404 as for a missing swap, so the response does not reveal that it exists.
            logger.LogInformation("swaps-api responded {Status} for swap {SwapId}", ex.StatusCode, swapId);
            return null;
        }
    }

    private async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(int swapId, CancellationToken ct)
    {
        try
        {
            var reply = await sessions.GetSessionsBySwapAsync(
                new GetSessionsBySwapRequest { SwapId = swapId }, deadline: Deadline(), cancellationToken: ct).ResponseAsync;
            return reply.ToDtos();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogInformation("sessions-api has no accepted swap {SwapId}", swapId);
            return [];
        }
    }

    private async Task<ConversationMessagesDto> GetConversationAsync(int swapId, CancellationToken ct)
    {
        try
        {
            var reply = await messages.GetSwapConversationAsync(
                new GetSwapConversationRequest { SwapId = swapId }, deadline: Deadline(), cancellationToken: ct).ResponseAsync;
            return reply.ToDto();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogInformation("messages-api has no conversation for swap {SwapId}", swapId);
            return ConversationMessagesDto.Empty;
        }
    }

    private static DateTime Deadline() => DateTime.UtcNow.Add(CallTimeout);

    private string ResolveParticipant(SwapDto swap)
    {
        var context = httpContextAccessor.HttpContext;
        var callerHeader = context?.Request.Headers[UserIdForwardingHandler.HeaderName].FirstOrDefault();
        var participantId = Guid.TryParse(callerHeader, out var callerId)
                            && (callerId == swap.InitiatorId || callerId == swap.PartnerId)
            ? callerId
            : swap.InitiatorId;

        if (context is not null)
            context.Items[UserIdForwardingHandler.ParticipantItemKey] = participantId.ToString();

        return participantId.ToString();
    }

    private async Task<T> SafeAsync<T>(
        string serviceName,
        Func<CancellationToken, Task<T>> call,
        T fallback,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            return await call(ct);
        }
        catch (RpcException ex) when (!ct.IsCancellationRequested && ex.StatusCode == StatusCode.PermissionDenied)
        {
            logger.LogWarning("{Service} denied access, returning empty data", serviceName);
            AddWarning(warnings, $"{serviceName} denied access");
            return fallback;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && IsUnavailable(ex))
        {
            logger.LogWarning(ex, "{Service} is unavailable, returning empty data", serviceName);
            AddWarning(warnings, $"{serviceName} is unavailable");
            return fallback;
        }
    }

    // Failures inside the HTTP pipeline, resilience included, reach the caller wrapped in an RpcException.
    private static bool IsUnavailable(Exception ex) => ex
        is RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Internal or StatusCode.Unknown }
        or HttpRequestException
        or TaskCanceledException
        or TimeoutRejectedException
        or BrokenCircuitException;

    private static void AddWarning(List<string> warnings, string warning)
    {
        lock (warnings)
            warnings.Add(warning);
    }

    private IReadOnlyList<T> KeepOwned<T>(
        IReadOnlyList<T> items, Func<T, bool> belongs, string kind, int swapId, List<string> warnings)
    {
        var owned = items.Where(belongs).ToList();
        var foreign = items.Count - owned.Count;
        if (foreign > 0)
        {
            logger.LogWarning("Skipped {Count} {Kind} that do not belong to swap {SwapId}", foreign, kind, swapId);
            warnings.Add($"{foreign} {kind} did not belong to swap {swapId} and were skipped");
        }

        return owned;
    }
}
