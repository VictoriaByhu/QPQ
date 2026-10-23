using QPQ.Moderation.Domain.Entities;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Dal.Interfaces;


namespace QPQ.Moderation.Dal;



public interface IReportAssignmentRepository : IRepository<ReportAssignment>
{
    // LINQ M:N: вибираємо скарги, призначені модератору після певної дати
    Task<IReadOnlyList<Report>> GetRecentlyAssignedReportsAsync(int moderatorId, DateTime since, CancellationToken ct = default);
}