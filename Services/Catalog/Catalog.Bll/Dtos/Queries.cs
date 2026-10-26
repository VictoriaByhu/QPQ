using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int page) => Math.Max(page, 1);
    public static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);
}

/// <summary>Параметри колекції навичок: сторінка, фільтри й сортування (sortBy: name | category | createdAt).</summary>
public sealed class SkillQuery
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, Paging.MaxPageSize)] public int PageSize { get; init; } = Paging.DefaultPageSize;
    [StringLength(100)] public string? Search { get; init; }
    public int? CategoryId { get; init; }
    [StringLength(50)] public string? Tag { get; init; }
    [RegularExpression("(?i)^(name|category|createdAt)$", ErrorMessage = "sortBy must be one of: name, category, createdAt.")]
    public string SortBy { get; init; } = "name";
    [RegularExpression("(?i)^(asc|desc)$", ErrorMessage = "sortDir must be asc or desc.")]
    public string SortDir { get; init; } = "asc";
}

public sealed class CategoryQuery
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, Paging.MaxPageSize)] public int PageSize { get; init; } = Paging.DefaultPageSize;
    [StringLength(100)] public string? Search { get; init; }
    [RegularExpression("(?i)^(asc|desc)$", ErrorMessage = "sortDir must be asc or desc.")]
    public string SortDir { get; init; } = "asc";
}
