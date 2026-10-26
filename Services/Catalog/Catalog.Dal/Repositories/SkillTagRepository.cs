using Catalog.Dal.Interfaces;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class SkillTagRepository(CatalogDbContext context) : GenericRepository<SkillTag>(context), ISkillTagRepository
{
    // M:N через проміжну сутність і її власне поле AddedAt.
    public async Task<IReadOnlyList<Skill>> GetRecentlyTaggedAsync(
        string tagName, DateTime since, CancellationToken ct = default)
        => await Set
            .Where(st => st.Tag.Name == tagName && st.AddedAt >= since && !st.Skill.IsDeleted)
            .OrderByDescending(st => st.AddedAt).ThenBy(st => st.SkillId)
            .Select(st => st.Skill)
            .AsNoTracking()
            .ToListAsync(ct);
}
