using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Catalog.Dal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly CatalogDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(CatalogDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await Set.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    // Update і Delete лише змінюють стан сутності в контексті, тому синхронні.
    public void Update(T entity) => Set.Update(entity);
    public void Delete(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct = default)
        => await Set.WithSpecification(specification).ToListAsync(ct);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct = default)
        => SpecificationEvaluator.Default.GetQuery(Set.AsQueryable(), specification, evaluateCriteriaOnly: true)
            .CountAsync(ct);

    public void SetOriginalRowVersion(T entity, byte[] rowVersion)
        => Context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
}
