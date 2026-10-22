using Sessions.Domain.Constants;
using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class Session
{
    public const int MaxDurationMinutes = 1440;
    public const int LinkMaxLength = 500;
    public const int LocationMaxLength = 200;
    public const int StatusMaxLength = 20;

    private readonly List<SessionSkill> _skills = [];

    public int Id { get; private set; }
    public int SwapId { get; private set; }
    public byte FormatId { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Link { get; private set; }
    public string? Location { get; private set; }
    public string Status { get; private set; } = SessionStatuses.Planned;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public AcceptedSwap Swap { get; private set; } = null!;
    public SessionFormat Format { get; private set; } = null!;
    public SessionOutcome? Outcome { get; private set; }
    public IReadOnlyCollection<SessionSkill> Skills => _skills;

    public int PlannedSkillMinutes => _skills.Sum(s => s.PlannedMinutes);

    private Session() { }

    public Session(
        int swapId, byte formatId, DateTime scheduledAt, int durationMinutes,
        string? link, string? location, Guid createdByUserId)
    {
        if (swapId <= 0)
            throw new DomainException("SwapId must be positive.");
        if (createdByUserId == Guid.Empty)
            throw new DomainException("Session author must be specified.");

        SwapId = swapId;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
        SetSchedule(scheduledAt, durationMinutes);
        SetPlace(formatId, link, location);
    }

    public void Reschedule(DateTime scheduledAt, int durationMinutes)
    {
        EnsurePlanned();
        SetSchedule(scheduledAt, durationMinutes);
        Touch();
    }

    public void ChangePlace(byte formatId, string? link, string? location)
    {
        EnsurePlanned();
        SetPlace(formatId, link, location);
        Touch();
    }

    public void AddSkill(int skillId, int plannedMinutes)
    {
        EnsurePlanned();
        if (_skills.Any(s => s.SkillId == skillId))
            throw new DomainException($"Skill {skillId} is already planned for this session.");
        if (PlannedSkillMinutes + plannedMinutes > DurationMinutes)
            throw new DomainException(
                $"Planned skill minutes ({PlannedSkillMinutes + plannedMinutes}) exceed the session duration ({DurationMinutes}).");

        _skills.Add(new SessionSkill(skillId, plannedMinutes));
        Touch();
    }

    public void RemoveSkill(int skillId)
    {
        EnsurePlanned();
        var skill = _skills.FirstOrDefault(s => s.SkillId == skillId)
                    ?? throw new NotFoundException($"Skill {skillId} is not planned for session {Id}.");

        _skills.Remove(skill);
        Touch();
    }

    public void Complete(string? summary, int actualDurationMinutes, DateTime completedAt)
    {
        EnsurePlanned();
        Outcome = new SessionOutcome(summary, actualDurationMinutes, completedAt);
        Status = SessionStatuses.Completed;
        Touch();
    }

    public void Cancel()
    {
        EnsurePlanned();
        Status = SessionStatuses.Cancelled;
        Touch();
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new DomainException($"Session {Id} is already deleted.");
        if (Status == SessionStatuses.Completed)
            throw new DomainException("A completed session cannot be deleted.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    private void SetSchedule(DateTime scheduledAt, int durationMinutes)
    {
        if (scheduledAt == default)
            throw new DomainException("Session time must be specified.");
        if (durationMinutes <= 0 || durationMinutes > MaxDurationMinutes)
            throw new DomainException($"Duration must be between 1 and {MaxDurationMinutes} minutes.");
        if (PlannedSkillMinutes > durationMinutes)
            throw new DomainException(
                $"Duration ({durationMinutes}) is shorter than the planned skill minutes ({PlannedSkillMinutes}).");

        ScheduledAt = scheduledAt;
        DurationMinutes = durationMinutes;
    }

    private void SetPlace(byte formatId, string? link, string? location)
    {
        link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

        switch (formatId)
        {
            case SessionFormatIds.Online:
                if (link is null)
                    throw new DomainException("An online session requires a link.");
                if (link.Length > LinkMaxLength)
                    throw new DomainException($"Link must not exceed {LinkMaxLength} characters.");
                location = null;
                break;
            case SessionFormatIds.InPerson:
                if (location is null)
                    throw new DomainException("An in-person session requires a location.");
                if (location.Length > LocationMaxLength)
                    throw new DomainException($"Location must not exceed {LocationMaxLength} characters.");
                link = null;
                break;
            default:
                throw new DomainException($"Unknown session format {formatId}.");
        }

        FormatId = formatId;
        Link = link;
        Location = location;
    }

    private void EnsurePlanned()
    {
        if (Status != SessionStatuses.Planned)
            throw new DomainException($"Session {Id} is {Status}; only planned sessions can be changed.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
