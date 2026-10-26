namespace Catalog.Dal.Interfaces;

/// <summary>
/// Об'єднує репозиторії з єдиним SaveChangesAsync: усі зміни бізнес-операції потрапляють у базу
/// однією транзакцією. Помилки бази перекладаються у доменні винятки.
/// </summary>
public interface IUnitOfWork
{
    ISkillRepository Skills { get; }
    ICategoryRepository Categories { get; }
    ITagRepository Tags { get; }
    ISkillTagRepository SkillTags { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
