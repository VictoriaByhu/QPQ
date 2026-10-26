namespace Catalog.Domain.Specifications;

/// <summary>Критерії вибірки навичок. Живе в Domain, щоб специфікація не залежала від шару BLL.</summary>
public sealed record SkillFilter(
    string? Search = null,
    int? CategoryId = null,
    string? Tag = null,
    SkillSortField SortBy = SkillSortField.Name,
    bool Descending = false,
    int Skip = 0,
    int Take = 20);

public enum SkillSortField { Name, Category, CreatedAt }
