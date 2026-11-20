namespace Platform.EventBus;

/// <summary>
/// Base of every event that crosses a service boundary. Events are immutable facts named in the past tense
/// and carry everything a consumer needs, so no consumer calls back into the publisher.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
