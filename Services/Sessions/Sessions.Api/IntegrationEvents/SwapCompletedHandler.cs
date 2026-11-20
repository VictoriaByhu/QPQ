using Platform.EventBus;
using Sessions.Api.IntegrationEvents.Events;

namespace Sessions.Api.IntegrationEvents;

/// <summary>Marks the copy Completed; new sessions can no longer be planned for it.</summary>
public sealed class SwapCompletedHandler(AcceptedSwapReplicator replicator) : IIntegrationEventHandler<SwapCompletedEvent>
{
    public Task HandleAsync(SwapCompletedEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapCompletedEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.CompletedAt, ct);
}
