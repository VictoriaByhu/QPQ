using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Specifications;

namespace QPQ.Moderation.Dal.Repositories;

public class ReportRepository : Repository<Report>, IReportRepository
{
    public ReportRepository(AppDbContext context) : base(context) { }

    public Task<Report?> GetReportWithDetailsAsync(long id, CancellationToken ct = default)
    {
        return _dbSet
            .Include(r => r.Detail)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    // Оновлено: повертає кортеж із даними та їх загальною кількістю
    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        // 1. Рахуємо загальну кількість ДО пагінації
        var totalCount = await _dbSet.CountAsync(ct);

        // 2. Отримуємо сторінку
        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    // Оновлено: повертає кортеж (використовується у ReportService)
    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedAsync(ISpecification<Report> spec, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        // 1. Формуємо запит із фільтром зі специфікації
        var query = _dbSet.Where(spec.Criteria);

        // 2. Рахуємо загальну кількість записів, що підходять під фільтр, ДО пагінації
        var totalCount = await query.CountAsync(ct);

        // 3. Завантажуємо лише потрібну сторінку (OFFSET ... FETCH)
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
    public async Task<Report?> GetWithExplicitLoadingAsync(long id, CancellationToken ct = default)
    {
        // 1-й запит: отримуємо лише основну сутність (скаргу)
        var report = await _dbSet.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report == null) return null;

        // 2-й запит: довантажуємо пов'язані дані окремо
        // Примітка: якщо в базовому класі контекст називається інакше, заміни Context на _context
        await _context.Entry(report).Collection(r => r.Attachments).LoadAsync(ct);

        return report;
    }
    public async Task<List<Report>> GetReportsByTagAssignedAfterAsync(long tagId, DateTime date, CancellationToken ct = default)
    {
        // LINQ-запит M:N через проміжну сутність та її власне поле
        return await _context.Set<ReportTag>()
            .Where(rt => rt.TagId == tagId && rt.AssignedAt >= date)
            .Select(rt => rt.Report)
            .ToListAsync(ct);
    }
}