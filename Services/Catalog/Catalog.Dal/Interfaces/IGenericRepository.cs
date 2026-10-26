using Ardalis.Specification;

namespace Catalog.Dal.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Вибірка за специфікацією: репозиторій лише інтерпретує її в LINQ для EF Core.</summary>
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct = default);

    /// <summary>Кількість записів за критеріями специфікації (без Skip/Take, Include й сортування).</summary>
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct = default);

    /// <summary>Повідомляє EF версію рядка, яку бачив клієнт (оптимістичний паралелізм).</summary>
    void SetOriginalRowVersion(T entity, byte[] rowVersion);
}
