using UserSkills.Domain.Models;

namespace UserSkills.Dal.Interfaces;

/// <summary>Спільні CRUD-операції. Конкретні інтерфейси додають лише специфічні запити.</summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<int> AddAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Локальна копія довідника навичок (читання). Оновлюється подіями Catalog.</summary>
public interface ISkillRepository : IGenericRepository<Skill>
{
}

/// <summary>
/// Довідник мов. Реалізований на чистому ADO.NET і свідомо НЕ успадковує IGenericRepository
/// (ключ — рядковий код, а не int Id).
/// </summary>
public interface ILanguageRepository
{
    Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default);
    Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Повертає ті коди з переданих, яких немає в довіднику.</summary>
    Task<IReadOnlyList<string>> GetUnknownCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct = default);
}

public interface IUserSkillRepository : IGenericRepository<UserSkill>
{
    /// <summary>Запис разом з деталями, мовами та доступністю (usp_GetUserSkillById).</summary>
    Task<UserSkill?> GetFullAsync(int id, CancellationToken ct = default);

    /// <summary>Список навичок користувача з фільтрами та пагінацією (usp_GetUserSkills).</summary>
    Task<IReadOnlyList<UserSkill>> ListByUserAsync(
        Guid userId, string? type, string? status, int pageNumber, int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Активні записи за навичкою (та, за потреби, типом) разом з назвою навички.
    /// Dapper multi-mapping: JOIN UserSkills + Skills, splitOn.
    /// </summary>
    Task<IReadOnlyList<UserSkill>> ListBySkillAsync(
        int skillId, string? type, int limit, CancellationToken ct = default);

    /// <summary>Створює запис, деталі, мови, доступність і рядок журналу (usp_AddUserSkill).</summary>
    Task<int> AddFullAsync(UserSkill entity, CancellationToken ct = default);

    /// <summary>Оновлює рівень, деталі, мови й доступність; повертає нову RowVersion (usp_UpdateUserSkill).</summary>
    Task<byte[]> UpdateFullAsync(
        int id, Guid updatedBy, byte[] rowVersion, UserSkill changes, CancellationToken ct = default);

    /// <summary>Змінює статус і пише журнал; повертає нову RowVersion (usp_ChangeUserSkillStatus).</summary>
    Task<byte[]> ChangeStatusAsync(
        int id, string newStatus, Guid changedBy, byte[] rowVersion, string? comment,
        CancellationToken ct = default);

    /// <summary>М'яке видалення власником з перевіркою RowVersion (usp_SoftDeleteUserSkill).</summary>
    Task SoftDeleteAsync(int id, Guid deletedBy, byte[] rowVersion, CancellationToken ct = default);
}

public interface IUserSkillStatusHistoryRepository
{
    Task<IReadOnlyList<UserSkillStatusHistoryEntry>> GetByUserSkillAsync(
        int userSkillId, CancellationToken ct = default);
}
