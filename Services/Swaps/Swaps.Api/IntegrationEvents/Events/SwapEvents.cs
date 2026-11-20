using Platform.EventBus;

namespace Swaps.Api.IntegrationEvents.Events;

// Contracts owned by Swaps. Consumers keep their own copies of these records (same class names and JSON),
// so no service references another one. Every event carries the participants, so a consumer can build
// its copy from any of them, whatever order they arrive in.

/// <summary>The partner accepted the swap; Sessions and Messages create their copies.</summary>
public sealed record SwapAcceptedEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime AcceptedAt, Guid ChangedByUserId)
    : IntegrationEvent;

/// <summary>An accepted swap was completed.</summary>
public sealed record SwapCompletedEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime CompletedAt, Guid ChangedByUserId)
    : IntegrationEvent;

/// <summary>An accepted swap was cancelled. Cancelling a pending swap publishes nothing: no copies exist yet.</summary>
public sealed record SwapCancelledEvent(
    int SwapId, Guid InitiatorId, Guid PartnerId, string Status, DateTime CancelledAt, Guid ChangedByUserId)
    : IntegrationEvent;

public static class SwapEventRoutingKeys
{
    public const string Accepted = "swap.accepted";
    public const string Completed = "swap.completed";
    public const string Cancelled = "swap.cancelled";
}
