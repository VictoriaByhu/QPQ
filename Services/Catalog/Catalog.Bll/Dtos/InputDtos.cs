using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Constants;

namespace Catalog.Bll.Dtos;

// Прості правила (обов'язковість, довжини, діапазони) — DataAnnotations;
// складніші (формат slug, URL, унікальність id) — FluentValidation у Validators/.

public sealed class CreateSkillDto
{
    [Required, StringLength(CatalogConstants.NameMaxLength)] public string Name { get; init; } = "";
    [Required, StringLength(CatalogConstants.SlugMaxLength)] public string Slug { get; init; } = "";
    [Range(1, int.MaxValue)] public int CategoryId { get; init; }
    [StringLength(CatalogConstants.DescriptionMaxLength)] public string? Description { get; init; }
    [StringLength(CatalogConstants.IconUrlMaxLength)] public string? IconUrl { get; init; }
    public List<int>? TagIds { get; init; }
}

public sealed class UpdateSkillDto
{
    [Required, StringLength(CatalogConstants.NameMaxLength)] public string Name { get; init; } = "";
    [Required, StringLength(CatalogConstants.SlugMaxLength)] public string Slug { get; init; } = "";
    [Range(1, int.MaxValue)] public int CategoryId { get; init; }
    [StringLength(CatalogConstants.DescriptionMaxLength)] public string? Description { get; init; }
    [StringLength(CatalogConstants.IconUrlMaxLength)] public string? IconUrl { get; init; }

    /// <summary>Версія рядка (base64) з останньої відповіді; якщо вказано — увімкнено перевірку паралелізму.</summary>
    public string? RowVersion { get; init; }
}

public sealed class UpdateSkillLevelDto
{
    [Required, StringLength(CatalogConstants.LevelTitleMaxLength)] public string Title { get; init; } = "";
    [StringLength(CatalogConstants.LevelDescriptionMaxLength)] public string? Description { get; init; }
}

public sealed class CreateCategoryDto
{
    [Required, StringLength(CatalogConstants.NameMaxLength)] public string Name { get; init; } = "";
    [Required, StringLength(CatalogConstants.SlugMaxLength)] public string Slug { get; init; } = "";
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
}

public sealed class UpdateCategoryDto
{
    [Required, StringLength(CatalogConstants.NameMaxLength)] public string Name { get; init; } = "";
    [Required, StringLength(CatalogConstants.SlugMaxLength)] public string Slug { get; init; } = "";
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public string? RowVersion { get; init; }
}

public sealed class CreateTagDto
{
    [Required, StringLength(CatalogConstants.TagNameMaxLength)] public string Name { get; init; } = "";
}
