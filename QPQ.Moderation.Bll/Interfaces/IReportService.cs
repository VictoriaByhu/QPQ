/*using QPQ.Moderation.Bll.DTOs;

namespace QPQ.Moderation.Bll.Interfaces;

public interface IReportService
{
    Task<ReportDto> GetReportAsync(long id, CancellationToken ct = default);
    Task ChangeReportStatusAsync(long id, string newStatus, CancellationToken ct = default);
}*/
using QPQ.Moderation.Bll.DTOs;

namespace QPQ.Moderation.Bll.Interfaces;

public interface IReportService
{
    Task<ReportDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ReportDto> CreateAsync(CreateReportDto dto, CancellationToken ct = default);
    Task<PagedResult<ReportDto>> GetPagedAsync(string? status, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<ReportDto> GetWithExplicitLoadingAsync(long id, CancellationToken ct = default);
}