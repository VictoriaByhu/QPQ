namespace Catalog.Bll.Dtos;

public sealed record SkillLevelDto(byte Level, string Title, string? Description);

public sealed record SkillBriefDto(int Id, string Name, string Slug);

public sealed record SkillListItemDto(
    int Id, string Name, string Slug, int CategoryId, string CategoryName, IReadOnlyList<string> Tags);

public sealed record SkillDto(
    int Id, string Name, string Slug, int CategoryId, string CategoryName,
    string? Description, string? IconUrl,
    IReadOnlyList<SkillLevelDto> Levels, IReadOnlyList<string> Tags,
    DateTime CreatedAt, DateTime? UpdatedAt, string RowVersion);

public sealed record CategoryDto(int Id, string Name, string Slug, int SortOrder, string RowVersion);

public sealed record CategoryWithSkillsDto(
    int Id, string Name, string Slug, int SortOrder, string RowVersion, IReadOnlyList<SkillBriefDto> Skills);

public sealed record TagDto(int Id, string Name);
