using MongoDB.Bson;
using Matching.Domain.Common;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.ValueObjects;

namespace Matching.Domain.Entities;

/// <summary>
/// Запис індексу підбору (колекція userSkillIndex): локальна копія запису UserSkills.
/// Offer і Want мають різні набори полів: <see cref="ExperienceYears"/> можливий лише в Offer.
/// Мови й вікна доступності — вбудовані масиви Value Objects.
/// </summary>
public sealed class UserSkillIndexEntry : BaseEntity
{
    private List<Language> _languages = new();
    private List<AvailabilitySlot> _availability = new();

    public int UserSkillId { get; private set; }
    public string UserId { get; private set; } = "";
    public int SkillId { get; private set; }
    public string SkillName { get; private set; } = "";
    public UserSkillType Type { get; private set; }
    public int Level { get; private set; }
    public UserSkillStatus Status { get; private set; }
    public int? ExperienceYears { get; private set; }
    public IReadOnlyList<Language> Languages => _languages.AsReadOnly();
    public IReadOnlyList<AvailabilitySlot> Availability => _availability.AsReadOnly();

    private UserSkillIndexEntry() { }                         // для десеріалізації

    public UserSkillIndexEntry(
        int userSkillId, string userId, int skillId, string skillName,
        UserSkillType type, int level, UserSkillStatus status, int? experienceYears,
        IEnumerable<Language> languages, IEnumerable<AvailabilitySlot> availability,
        ObjectId? id = null, DateTime? createdAt = null) : base(id, createdAt)
    {
        if (!Enum.IsDefined(type)) throw new DomainException("Unknown skill type");

        UserSkillId = Guard.Positive(userSkillId, "UserSkillId");
        UserId = Guard.UserId(userId, "UserId");
        SkillId = Guard.Positive(skillId, "SkillId");
        Type = type;
        Apply(skillName, level, status, experienceYears, languages, availability);
        UpdatedAt = CreatedAt;
    }

    /// <summary>Оновлення за подією UserSkillsChanged. Тип, власник і навичка незмінні.</summary>
    public void Update(
        string skillName, int level, UserSkillStatus status, int? experienceYears,
        IEnumerable<Language> languages, IEnumerable<AvailabilitySlot> availability)
    {
        Apply(skillName, level, status, experienceYears, languages, availability);
        Touch();
    }

    private void Apply(
        string skillName, int level, UserSkillStatus status, int? experienceYears,
        IEnumerable<Language> languages, IEnumerable<AvailabilitySlot> availability)
    {
        if (!Enum.IsDefined(status)) throw new DomainException("Unknown status");
        if (experienceYears is not null)
        {
            if (Type != UserSkillType.Offer)
                throw new DomainException("ExperienceYears is allowed only for Offer");
            Guard.InRange(experienceYears.Value, 0, 80, "ExperienceYears");
        }

        var langs = languages.ToList();
        if (langs.Count > 10) throw new DomainException("At most 10 languages are allowed");
        if (langs.Select(l => l.Code).Distinct().Count() != langs.Count)
            throw new DomainException("Language codes must be unique");

        var slots = availability.Distinct().ToList();
        if (slots.Count > 21) throw new DomainException("At most 21 availability slots are allowed");

        SkillName = Guard.Text(skillName, 100, "SkillName");
        Level = Guard.InRange(level, 1, 5, "Level");
        Status = status;
        ExperienceYears = experienceYears;
        _languages = langs;
        _availability = slots;
    }
}
