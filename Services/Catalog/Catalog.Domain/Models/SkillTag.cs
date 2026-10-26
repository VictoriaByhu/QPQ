using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Models;

/// <summary>Проміжна сутність M:N Skill <-> Tag із власним полем AddedAt.</summary>
public class SkillTag
{
    public int SkillId { get; private set; }
    public Skill Skill { get; private set; } = null!;
    public int TagId { get; private set; }
    public Tag Tag { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }

    private SkillTag() { }

    internal SkillTag(int tagId)
    {
        if (tagId <= 0) throw new DomainException("TagId must be positive.");
        TagId = tagId;
        AddedAt = DateTime.UtcNow;
    }
}
