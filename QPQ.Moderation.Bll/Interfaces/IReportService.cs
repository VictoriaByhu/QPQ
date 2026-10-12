using QPQ.Moderation.Bll.DTOs;

namespace QPQ.Moderation.Bll.Interfaces;

public interface IReportService
{
    Task<ReportDto> GetReportAsync(long id, CancellationToken ct = default);
    Task ChangeReportStatusAsync(long id, string newStatus, CancellationToken ct = default);
}