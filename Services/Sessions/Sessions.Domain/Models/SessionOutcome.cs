using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class SessionOutcome
{
    public const int SummaryMaxLength = 1000;

    public int SessionId { get; private set; }
    public string? Summary { get; private set; }
    public int ActualDurationMinutes { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Session Session { get; private set; } = null!;

    private SessionOutcome() { }

    internal SessionOutcome(string? summary, int actualDurationMinutes, DateTime completedAt)
    {
        if (actualDurationMinutes <= 0 || actualDurationMinutes > Session.MaxDurationMinutes)
            throw new DomainException($"Actual duration must be between 1 and {Session.MaxDurationMinutes} minutes.");

        summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        if (summary?.Length > SummaryMaxLength)
            throw new DomainException($"Summary must not exceed {SummaryMaxLength} characters.");

        Summary = summary;
        ActualDurationMinutes = actualDurationMinutes;
        CompletedAt = completedAt;
    }
}
