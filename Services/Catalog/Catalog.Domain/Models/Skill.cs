using Catalog.Domain.Constants;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Models;

/// <summary>
/// Навичка (SkillId у решті контекстів QPQ). Бізнес-правила виконують методи сутності,
/// публічних setter-ів немає.
/// Зв'язки: 1:1 Skill-SkillDetail, 1:N Skill-SkillLevel, M:N Skill-Tag через SkillTag.
/// </summary>
public class Skill
{
    public int Id { get; private set; }
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Category Category { get; private set; } = null!;
    public SkillDetail? Detail { get; private set; }                       // 1:1
    public List<SkillLevel> Levels { get; } = new();                       // 1:N
    public List<SkillTag> SkillTags { get; } = new();                      // M:N

    private Skill() { }                                      // для EF Core

    public Skill(int categoryId, string name, string slug)
    {
        if (categoryId <= 0) throw new DomainException("CategoryId must be positive.");
        CategoryId = categoryId;
        Name = Guard.Name(name, "Skill name");
        Slug = Guard.Slug(slug);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int categoryId, string name, string slug)
    {
        EnsureNotDeleted();
        if (categoryId <= 0) throw new DomainException("CategoryId must be positive.");
        CategoryId = categoryId;
        Name = Guard.Name(name, "Skill name");
        Slug = Guard.Slug(slug);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Створює або оновлює опис навички (1:1).</summary>
    public void SetDetail(string? description, string? iconUrl)
    {
        EnsureNotDeleted();
        if (Detail is null) Detail = new SkillDetail(description, iconUrl);
        else Detail.Update(description, iconUrl);
    }

    /// <summary>Додає п'ять рівнів єдиної шкали 1..5; наявні рівні не чіпає.</summary>
    public void AddDefaultLevels()
    {
        EnsureNotDeleted();
        for (var i = 0; i < CatalogConstants.DefaultLevelTitles.Length; i++)
        {
            var level = (byte)(CatalogConstants.MinLevel + i);
            if (Levels.All(l => l.Level != level))
                Levels.Add(new SkillLevel(level, CatalogConstants.DefaultLevelTitles[i], null));
        }
    }

    public SkillLevel RenameLevel(byte level, string title, string? description)
    {
        EnsureNotDeleted();
        var existing = Levels.FirstOrDefault(l => l.Level == level)
                       ?? throw new EntityNotFoundException($"Skill {Id} has no level {level}.");
        existing.Update(title, description);
        UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    public void AddTag(int tagId)
    {
        EnsureNotDeleted();
        if (tagId <= 0) throw new DomainException("TagId must be positive.");
        if (SkillTags.Any(st => st.TagId == tagId))
            throw new DomainException($"Skill already has tag {tagId}.");
        SkillTags.Add(new SkillTag(tagId));
    }

    public void RemoveTag(int tagId)
    {
        EnsureNotDeleted();
        var link = SkillTags.FirstOrDefault(st => st.TagId == tagId)
                   ?? throw new EntityNotFoundException($"Skill {Id} has no tag {tagId}.");
        SkillTags.Remove(link);
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("Skill is deleted.");
    }
}
