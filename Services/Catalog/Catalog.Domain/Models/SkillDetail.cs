using Catalog.Domain.Constants;

namespace Catalog.Domain.Models;

/// <summary>1:1 із Skill: первинний ключ SkillId є водночас зовнішнім.</summary>
public class SkillDetail
{
    public int SkillId { get; private set; }
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private SkillDetail() { }

    public SkillDetail(string? description, string? iconUrl)
    {
        Apply(description, iconUrl);
    }

    public void Update(string? description, string? iconUrl)
    {
        Apply(description, iconUrl);
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(string? description, string? iconUrl)
    {
        Description = Guard.Optional(description, "Description", CatalogConstants.DescriptionMaxLength);
        IconUrl = Guard.Optional(iconUrl, "IconUrl", CatalogConstants.IconUrlMaxLength);
    }
}
