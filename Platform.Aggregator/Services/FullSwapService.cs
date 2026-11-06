using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Platform.Aggregator.Services;

public sealed class FullSwapService(
    SwapsClient swaps,
    SessionsClient sessions,
    MessagesClient messages,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FullSwapService> logger)
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");

    public async Task<FullSwapDto?> GetFullSwapAsync(int swapId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("AggregateFullSwap");
        activity?.SetTag("swap.id", swapId);

        var swap = await swaps.GetSwapAsync(swapId, ct);
        if (swap is null)
            return null;

        var participantId = ResolveParticipant(swap);
        activity?.SetTag("user.id", participantId);

        var warnings = new List<string>();
        var sessionsTask = SafeAsync("sessions-api",
            token => sessions.GetSessionsForSwapAsync(swapId, token), [], warnings, ct);
        var messagesTask = SafeAsync("messages-api",
            token => messages.GetMessagesForSwapAsync(swapId, token), ConversationMessagesDto.Empty, warnings, ct);
        await Task.WhenAll(sessionsTask, messagesTask);

        var swapSessions = KeepOwned(await sessionsTask, s => s.SwapId == swapId, "sessions", swapId, warnings);
        var conversation = await messagesTask;
        var swapMessages = conversation.Conversation?.SwapId == swapId
            ? KeepOwned(conversation.Messages, m => m.ConversationId == conversation.Conversation.Id, "messages", swapId, warnings)
            : KeepOwned(conversation.Messages, _ => false, "messages", swapId, warnings);

        logger.LogInformation("Aggregated swap {SwapId} for {UserId}: {SessionCount} sessions, {MessageCount} messages, {WarningCount} warnings",
            swapId, participantId, swapSessions.Count, swapMessages.Count, warnings.Count);

        return new FullSwapDto(swap, swapSessions, swapMessages, warnings);
    }

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
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException
                                       or TaskCanceledException
                                       or TimeoutRejectedException
                                       or BrokenCircuitException)
        {
            logger.LogWarning(ex, "{Service} is unavailable, returning empty data", serviceName);
            lock (warnings)
                warnings.Add($"{serviceName} is unavailable");
            return fallback;
        }
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
