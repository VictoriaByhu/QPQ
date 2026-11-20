using Messages.WebApi.IntegrationEvents.Events;
using Platform.EventBus;

namespace Messages.WebApi.IntegrationEvents;

/// <summary>Marks the conversation of the swap Completed and posts the system message.</summary>
public sealed class SwapCompletedHandler(ConversationReplicator replicator) : IIntegrationEventHandler<SwapCompletedEvent>
{
    public Task HandleAsync(SwapCompletedEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapCompletedEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.CompletedAt, e.ChangedByUserId, ct);
}
