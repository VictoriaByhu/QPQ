using System.Data;
using QPQ.Moderation.Dal.Interfaces; // “ут лежать IReportRepository та IViolationTypeRepository

namespace QPQ.Moderation.Dal;

public interface IUnitOfWork : IAsyncDisposable
{
    IReportRepository Reports { get; }
    IViolationTypeRepository ViolationTypes { get; } // јбо ≥нший тв≥й репозитор≥й

    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}