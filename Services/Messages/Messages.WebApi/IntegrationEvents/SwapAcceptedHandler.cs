using Messages.WebApi.IntegrationEvents.Events;
using Platform.EventBus;

namespace Messages.WebApi.IntegrationEvents;

/// <summary>Opens the conversation of a newly accepted swap.</summary>
public sealed class SwapAcceptedHandler(ConversationReplicator replicator) : IIntegrationEventHandler<SwapAcceptedEvent>
{
    public Task HandleAsync(SwapAcceptedEvent e, CancellationToken ct)
        => replicator.ApplyAsync(e.EventId, nameof(SwapAcceptedEvent), e.SwapId, e.InitiatorId, e.PartnerId,
            e.Status, e.AcceptedAt, e.ChangedByUserId, ct);
}
