using Platform.EventBus;

namespace Messages.WebApi.IntegrationEvents.Events;

// Messages' own copies of the contracts published by Swaps (matched by class name and JSON shape).

public sealed record SwapAcceptedEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime AcceptedAt, Guid ChangedByUserId)
    : IntegrationEvent;

public sealed record SwapCompletedEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime CompletedAt, Guid ChangedByUserId)
    : IntegrationEvent;

public sealed record SwapCancelledEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime CancelledAt, Guid ChangedByUserId)
    : IntegrationEvent;
