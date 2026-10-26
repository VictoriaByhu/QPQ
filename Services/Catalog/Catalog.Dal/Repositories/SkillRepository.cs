using Catalog.Dal.Interfaces;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class SkillRepository(CatalogDbContext context) : GenericRepository<Skill>(context), ISkillRepository
{
    // Eager Loading: усе пов'язане завантажується одним запитом із JOIN.
    public Task<Skill?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(s => s.Category)
              .Include(s => s.Detail)
              .Include(s => s.Levels)
              .Include(s => s.SkillTags).ThenInclude(st => st.Tag)
              .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    public Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken ct = default)
        => Set.AnyAsync(s => !s.IsDeleted && s.Name == name && s.Id != exceptId, ct);

    public Task<bool> ExistsBySlugAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => Set.AnyAsync(s => !s.IsDeleted && s.Slug == slug && s.Id != exceptId, ct);
}
