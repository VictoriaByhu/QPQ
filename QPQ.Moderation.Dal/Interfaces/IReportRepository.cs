using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Interfaces;

public interface IReportRepository : IGenericRepository<Report>
{
    // Виклик збережуваної процедури з Лаб. 01
    Task ChangeStatusAsync(long reportId, string newStatus, CancellationToken ct = default);
}