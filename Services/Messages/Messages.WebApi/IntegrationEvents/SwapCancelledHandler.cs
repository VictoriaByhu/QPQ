using Messages.WebApi.IntegrationEvents.Events;
using Platform.EventBus;

namespace Messages.WebApi.IntegrationEvents;

/// <summary>Marks the conversation of the swap Cancelled (read-only from then on) and posts the system message.</summary>
public sealed class SwapCancelledHandler(ConversationReplicator replicator) : IIntegrationEventHandler<SwapCancelledEvent>
{
    public Task HandleAsync(SwapCancelledEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapCancelledEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.CancelledAt, e.ChangedByUserId, ct);
}
