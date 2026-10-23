using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Dal.Repositories;

namespace QPQ.Moderation.Dal.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Reports = new ReportRepository(_context);
        Moderators = new ModeratorRepository(_context);
        ReportAssignments = new ReportAssignmentRepository(_context);
    }

    public IReportRepository Reports { get; }
    public IModeratorRepository Moderators { get; }
    public IReportAssignmentRepository ReportAssignments { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}