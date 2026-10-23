using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Перевіряємо, чи є вже дані
        if (await db.Set<Report>().AnyAsync(ct))
            return;

        // Створюємо тестову скаргу через публічний конструктор
        var testReport = new Report(1, "TestUser", "Детальний опис скарги", "Тестова скарга на спам");

        db.Set<Report>().Add(testReport);
        await db.SaveChangesAsync(ct);
    }
}