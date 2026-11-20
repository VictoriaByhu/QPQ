using Platform.EventBus;
using Sessions.Api.IntegrationEvents.Events;

namespace Sessions.Api.IntegrationEvents;

/// <summary>Marks the copy Cancelled; new sessions can no longer be planned for it.</summary>
public sealed class SwapCancelledHandler(AcceptedSwapReplicator replicator) : IIntegrationEventHandler<SwapCancelledEvent>
{
    public Task HandleAsync(SwapCancelledEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapCancelledEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.CancelledAt, ct);
}
