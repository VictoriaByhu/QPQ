using Platform.EventBus;
using Platform.ServiceDefaults;
using Swaps.Api.IntegrationEvents.Events;
using Swaps.Bll.Caching;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;
using Swaps.Domain.Constants;

namespace Swaps.Api.IntegrationEvents;

/// <summary>
/// Publishes swap events only after the inner service has committed the change: if the change fails,
/// the exception propagates and nothing is published. A publish failure cannot undo the committed change,
/// so it is logged as an error and the request still succeeds (see README, Lab 08: outbox).
/// </summary>
public sealed class PublishingSwapService(
    ISwapService inner,
    ISwapCardReader cards,
    IEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PublishingSwapService> logger) : ISwapService
{
    // A request must not hang on an unreachable broker; the change is already saved anyway.
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(5);

    public async Task AcceptAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.AcceptAsync(userId, swapId, input, ct);
        await PublishAsync(swapId, SwapEventRoutingKeys.Accepted, swap =>
            new SwapAcceptedEvent(swap.Id, swap.InitiatorId, swap.PartnerId, SwapStatuses.Accepted, Now(), userId));
    }

    public async Task CompleteAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.CompleteAsync(userId, swapId, input, ct);
        await PublishAsync(swapId, SwapEventRoutingKeys.Completed, swap =>
            new SwapCompletedEvent(swap.Id, swap.InitiatorId, swap.PartnerId, SwapStatuses.Completed, Now(), userId));
    }

    public async Task CancelAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        // Only an accepted swap has copies in Sessions and Messages.
        var before = await cards.GetCardAsync(swapId, ct);
        await inner.CancelAsync(userId, swapId, input, ct);

        if (before?.Status == SwapStatuses.Accepted)
            await PublishAsync(swapId, SwapEventRoutingKeys.Cancelled, swap =>
                new SwapCancelledEvent(swap.Id, swap.InitiatorId, swap.PartnerId, SwapStatuses.Cancelled, Now(), userId));
    }

    public Task<SwapDto> CreateAsync(Guid userId, CreateSwapDto input, CancellationToken ct = default)
        => inner.CreateAsync(userId, input, ct);

    public Task<SwapDto> GetByIdAsync(Guid userId, int swapId, CancellationToken ct = default)
        => inner.GetByIdAsync(userId, swapId, ct);

    public Task<IReadOnlyList<SwapListItemDto>> GetMineAsync(Guid userId, SwapQueryDto query, CancellationToken ct = default)
        => inner.GetMineAsync(userId, query, ct);

    public Task<SwapDto> UpdateDetailsAsync(Guid userId, int swapId, UpdateSwapDetailsDto input, CancellationToken ct = default)
        => inner.UpdateDetailsAsync(userId, swapId, input, ct);

    public Task DeleteAsync(Guid userId, int swapId, CancellationToken ct = default)
        => inner.DeleteAsync(userId, swapId, ct);

    public Task RejectAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => inner.RejectAsync(userId, swapId, input, ct);

    public Task<IReadOnlyList<SwapStatusHistoryDto>> GetHistoryAsync(Guid userId, int swapId, CancellationToken ct = default)
        => inner.GetHistoryAsync(userId, swapId, ct);

    private static DateTime Now() => DateTime.UtcNow;

    private async Task PublishAsync<TEvent>(int swapId, string routingKey, Func<SwapDto, TEvent> create)
        where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent).Name;
        var correlationId = httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();

        try
        {
            // The cache entry was invalidated by the change, so this reads the committed state.
            var swap = await cards.GetCardAsync(swapId, CancellationToken.None)
                       ?? throw new InvalidOperationException($"Swap {swapId} was not found after the change.");

            var @event = create(swap) with { CorrelationId = correlationId };

            using var timeout = new CancellationTokenSource(PublishTimeout);
            await publisher.PublishAsync(@event, Topology.SwapsExchange, routingKey, timeout.Token);

            logger.LogInformation("Published {EventType} {EventId} to {Exchange}/{RoutingKey} for swap {SwapId}, correlation {CorrelationId}",
                eventType, @event.EventId, Topology.SwapsExchange, routingKey, swapId, correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish {EventType} for swap {SwapId}, correlation {CorrelationId}: the change is saved, but Sessions and Messages will not see it",
                eventType, swapId, correlationId);
        }
    }
}
