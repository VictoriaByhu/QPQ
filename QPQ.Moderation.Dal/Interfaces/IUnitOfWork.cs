namespace QPQ.Moderation.Dal.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IReportRepository Reports { get; }
    IModeratorRepository Moderators { get; }
    IReportAssignmentRepository ReportAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}