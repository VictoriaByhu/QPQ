using Ardalis.Specification;
using Catalog.Domain.Models;

namespace Catalog.Domain.Specifications;

/// <summary>Друга специфікація з іншими критеріями: активні навички однієї категорії за назвою.</summary>
public sealed class SkillsByCategorySpec : Specification<Skill>
{
    public SkillsByCategorySpec(int categoryId)
    {
        Query.Where(s => s.CategoryId == categoryId && !s.IsDeleted)
             .OrderBy(s => s.Name);
    }
}
