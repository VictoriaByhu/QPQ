using Matching.Domain.Common;
using Matching.Domain.ValueObjects;

namespace Matching.Domain.Entities;

/// <summary>
/// Похідна колекція skillStats: лічильники попиту й пропозиції за активними записами.
/// Власного життєвого циклу не має (перераховується агрегацією), тому не успадковує BaseEntity;
/// _id збігається зі SkillId.
/// </summary>
public sealed class SkillStats
{
    private List<LanguageCount> _topLanguages = new();

    public int SkillId { get; private set; }
    public string SkillName { get; private set; } = "";
    public int OffersCount { get; private set; }
    public int WantsCount { get; private set; }
    public IReadOnlyList<LanguageCount> TopLanguages => _topLanguages.AsReadOnly();
    public DateTime UpdatedAt { get; private set; }

    private SkillStats() { }                                  // для десеріалізації

    public SkillStats(int skillId, string skillName, int offersCount, int wantsCount,
        IEnumerable<LanguageCount> topLanguages, DateTime updatedAt)
    {
        SkillId = Guard.Positive(skillId, "SkillId");
        SkillName = Guard.Text(skillName, 100, "SkillName");
        OffersCount = offersCount < 0 ? throw new Exceptions.DomainException("OffersCount must be >= 0") : offersCount;
        WantsCount = wantsCount < 0 ? throw new Exceptions.DomainException("WantsCount must be >= 0") : wantsCount;
        _topLanguages = topLanguages.Take(3).ToList();
        UpdatedAt = updatedAt;
    }
}
