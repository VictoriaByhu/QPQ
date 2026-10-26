using Catalog.Dal.Interfaces;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class TagRepository(CatalogDbContext context) : GenericRepository<Tag>(context), ITagRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => Set.AnyAsync(t => t.Name == name, ct);
}
