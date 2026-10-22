using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class SessionSkill
{
    public int SessionId { get; private set; }
    public int SkillId { get; private set; }
    public int PlannedMinutes { get; private set; }
    public DateTime AddedAt { get; private set; }

    public Session Session { get; private set; } = null!;
    public Skill Skill { get; private set; } = null!;

    private SessionSkill() { }

    internal SessionSkill(int skillId, int plannedMinutes)
    {
        if (skillId <= 0)
            throw new DomainException("SkillId must be positive.");
        if (plannedMinutes <= 0)
            throw new DomainException("Planned minutes must be positive.");

        SkillId = skillId;
        PlannedMinutes = plannedMinutes;
        AddedAt = DateTime.UtcNow;
    }
}
