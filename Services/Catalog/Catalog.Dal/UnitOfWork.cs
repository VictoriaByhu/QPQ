using Catalog.Dal.Interfaces;
using Catalog.Dal.Repositories;
using Catalog.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
        Skills = new SkillRepository(context);
        Categories = new CategoryRepository(context);
        Tags = new TagRepository(context);
        SkillTags = new SkillTagRepository(context);
    }

    public ISkillRepository Skills { get; }
    public ICategoryRepository Categories { get; }
    public ITagRepository Tags { get; }
    public ISkillTagRepository SkillTags { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was modified or deleted by another user. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            // Запасний захист на випадок гонки між перевіркою в сервісі та вставкою.
            throw sql.Number switch
            {
                2601 or 2627 => new ConflictException("A record with the same unique value already exists."),
                547 => new ConflictException("The operation violates a relationship with other records."),
                _ => ex
            };
        }
    }
}
