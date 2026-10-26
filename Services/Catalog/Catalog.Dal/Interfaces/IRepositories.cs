using Catalog.Domain.Models;

namespace Catalog.Dal.Interfaces;

public interface ISkillRepository : IGenericRepository<Skill>
{
    /// <summary>Eager Loading: навичка з категорією, описом, рівнями й тегами одним запитом.</summary>
    Task<Skill?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, int? exceptId = null, CancellationToken ct = default);
}

public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>Explicit Loading: категорія одним запитом, її навички окремим — лише коли потрібні.</summary>
    Task<Category?> GetWithSkillsAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<bool> HasActiveSkillsAsync(int id, CancellationToken ct = default);
}

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}

public interface ISkillTagRepository : IGenericRepository<SkillTag>
{
    /// <summary>LINQ to Entities: навички з тегом, доданим не раніше since (через проміжну сутність M:N).</summary>
    Task<IReadOnlyList<Skill>> GetRecentlyTaggedAsync(string tagName, DateTime since, CancellationToken ct = default);
}
