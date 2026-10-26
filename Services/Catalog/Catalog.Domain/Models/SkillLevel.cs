using Catalog.Domain.Constants;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Models;

/// <summary>1:N Skill -> SkillLevel: назва рівня володіння 1..5 для кожної навички.</summary>
public class SkillLevel
{
    public int Id { get; private set; }
    public int SkillId { get; private set; }
    public byte Level { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }

    private SkillLevel() { }

    public SkillLevel(byte level, string title, string? description)
    {
        if (level is < CatalogConstants.MinLevel or > CatalogConstants.MaxLevel)
            throw new DomainException($"Level must be between {CatalogConstants.MinLevel} and {CatalogConstants.MaxLevel}.");
        Level = level;
        Update(title, description);
    }

    public void Update(string title, string? description)
    {
        Title = Guard.Name(title, "Level title", CatalogConstants.LevelTitleMaxLength);
        Description = Guard.Optional(description, "Level description", CatalogConstants.LevelDescriptionMaxLength);
    }
}
