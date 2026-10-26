using Catalog.Dal.Interfaces;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository(CatalogDbContext context) : GenericRepository<Category>(context), ICategoryRepository
{
    // Explicit Loading: два окремих SQL-запити (категорія, потім її активні навички).
    public async Task<Category?> GetWithSkillsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);   // 1-й запит
        if (category is null) return null;

        await Context.Entry(category).Collection(c => c.Skills)
            .Query().Where(s => !s.IsDeleted).OrderBy(s => s.Name)
            .LoadAsync(ct);                                                                  // 2-й запит
        return category;
    }

    public Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken ct = default)
        => Set.AnyAsync(c => !c.IsDeleted && c.Name == name && c.Id != exceptId, ct);

    public Task<bool> ExistsBySlugAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => Set.AnyAsync(c => !c.IsDeleted && c.Slug == slug && c.Id != exceptId, ct);

    public Task<bool> HasActiveSkillsAsync(int id, CancellationToken ct = default)
        => Context.Skills.AnyAsync(s => s.CategoryId == id && !s.IsDeleted, ct);
}
