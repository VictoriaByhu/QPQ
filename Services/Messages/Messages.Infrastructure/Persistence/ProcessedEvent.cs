using MongoDB.Bson;

namespace Messages.Infrastructure.Persistence;

/// <summary>An integration event already applied to this database; a unique index on eventId rejects a second insert.</summary>
public sealed class ProcessedEvent
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
