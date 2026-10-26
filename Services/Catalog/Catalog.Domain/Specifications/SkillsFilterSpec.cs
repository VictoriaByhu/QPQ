using Ardalis.Specification;
using Catalog.Domain.Models;

namespace Catalog.Domain.Specifications;

/// <summary>
/// Список навичок: пошук, категорія, тег, сортування, сторінка й потрібні Include.
/// Не залежить від EF: специфікацію можна перевірити на звичайній колекції (spec.Evaluate).
/// </summary>
public sealed class SkillsFilterSpec : Specification<Skill>
{
    /// <param name="paged">false — лише критерії (для підрахунку totalCount без Skip/Take).</param>
    public SkillsFilterSpec(SkillFilter filter, bool paged = true)
    {
        Query.Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            Query.Where(s => s.Name.Contains(filter.Search));
        if (filter.CategoryId is not null)
            Query.Where(s => s.CategoryId == filter.CategoryId);
        if (!string.IsNullOrWhiteSpace(filter.Tag))
            Query.Where(s => s.SkillTags.Any(st => st.Tag.Name == filter.Tag));

        if (!paged) return;

        Query.Include(s => s.Category)
             .Include(s => s.SkillTags).ThenInclude(st => st.Tag);

        // Лише дозволені поля сортування; Id додається для стабільного порядку між сторінками.
        switch (filter.SortBy, filter.Descending)
        {
            case (SkillSortField.Category, true):
                Query.OrderByDescending(s => s.Category.Name).ThenBy(s => s.Id); break;
            case (SkillSortField.Category, false):
                Query.OrderBy(s => s.Category.Name).ThenBy(s => s.Id); break;
            case (SkillSortField.CreatedAt, true):
                Query.OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id); break;
            case (SkillSortField.CreatedAt, false):
                Query.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id); break;
            case (_, true):
                Query.OrderByDescending(s => s.Name).ThenBy(s => s.Id); break;
            default:
                Query.OrderBy(s => s.Name).ThenBy(s => s.Id); break;
        }

        Query.Skip(filter.Skip).Take(filter.Take);
    }
}
