namespace Sessions.Dal.Entities;

/// <summary>An integration event already applied to this database; its id makes redelivery harmless.</summary>
public class ProcessedEvent
{
    public const int EventTypeMaxLength = 100;

    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string EventType { get; set; } = string.Empty;
}
