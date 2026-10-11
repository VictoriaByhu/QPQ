using System.Data;

namespace UserSkills.Dal.Interfaces;

/// <summary>
/// Одне з'єднання і одна транзакція на бізнес-операцію: усі репозиторії працюють через них.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ISkillRepository Skills { get; }
    ILanguageRepository Languages { get; }
    IUserSkillRepository UserSkills { get; }
    IUserSkillStatusHistoryRepository StatusHistory { get; }

    Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
