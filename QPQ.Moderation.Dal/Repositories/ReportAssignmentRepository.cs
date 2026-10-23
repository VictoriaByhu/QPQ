using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Entities;

namespace QPQ.Moderation.Dal.Repositories;

public class ReportAssignmentRepository : Repository<ReportAssignment>, IReportAssignmentRepository
{
    public ReportAssignmentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Report>> GetRecentlyAssignedReportsAsync(int moderatorId, DateTime since, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(ra => ra.ModeratorId == moderatorId && ra.AssignedAt >= since)
            .OrderByDescending(ra => ra.AssignedAt)
            .Select(ra => ra.Report!)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}