using Platform.EventBus;
using Sessions.Api.IntegrationEvents.Events;

namespace Sessions.Api.IntegrationEvents;

/// <summary>Creates the AcceptedSwaps row for a newly accepted swap.</summary>
public sealed class SwapAcceptedHandler(AcceptedSwapReplicator replicator) : IIntegrationEventHandler<SwapAcceptedEvent>
{
    public Task HandleAsync(SwapAcceptedEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapAcceptedEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.AcceptedAt, ct);
}
