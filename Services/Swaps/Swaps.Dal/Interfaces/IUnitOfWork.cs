using System.Data;

namespace Swaps.Dal.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    ISwapRepository Swaps { get; }
    ISkillRepository Skills { get; }
    ISwapStatusHistoryRepository History { get; }

    Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
