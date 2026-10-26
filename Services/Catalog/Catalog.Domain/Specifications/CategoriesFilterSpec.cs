using Ardalis.Specification;
using Catalog.Domain.Models;

namespace Catalog.Domain.Specifications;

public sealed record CategoryFilter(string? Search = null, bool Descending = false, int Skip = 0, int Take = 20);

public sealed class CategoriesFilterSpec : Specification<Category>
{
    public CategoriesFilterSpec(CategoryFilter filter, bool paged = true)
    {
        Query.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            Query.Where(c => c.Name.Contains(filter.Search));

        if (!paged) return;

        if (filter.Descending) Query.OrderByDescending(c => c.SortOrder).ThenBy(c => c.Id);
        else Query.OrderBy(c => c.SortOrder).ThenBy(c => c.Id);

        Query.Skip(filter.Skip).Take(filter.Take);
    }
}
