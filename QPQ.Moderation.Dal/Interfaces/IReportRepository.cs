using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Specifications;

namespace QPQ.Moderation.Dal.Interfaces;

public interface IReportRepository : IRepository<Report>
{
    // Твій метод для Eager Loading
    Task<Report?> GetReportWithDetailsAsync(long id, CancellationToken ct = default);

    // Метод для пагінації та фільтрації, який приймає специфікацію
    Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedAsync(ISpecification<Report> spec, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<Report?> GetWithExplicitLoadingAsync(long id, CancellationToken ct = default);
    Task<List<Report>> GetReportsByTagAssignedAfterAsync(long tagId, DateTime date, CancellationToken ct = default);
}