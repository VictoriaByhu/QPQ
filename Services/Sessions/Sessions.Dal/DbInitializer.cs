using Microsoft.EntityFrameworkCore;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Dal;

public static class DbInitializer
{
    private static readonly Guid U1 = Guid.Parse("3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01");
    private static readonly Guid U2 = Guid.Parse("7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02");
    private static readonly Guid U3 = Guid.Parse("b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03");

    public static async Task SeedAsync(SessionsDbContext db, CancellationToken ct = default)
    {
        await SeedAcceptedSwapsAsync(db, ct);
        await SeedSessionsAsync(db, ct);
    }

    private static async Task SeedAcceptedSwapsAsync(SessionsDbContext db, CancellationToken ct)
    {
        AcceptedSwap[] swaps =
        [
            new(2, U2, U3, AcceptedSwapStatuses.Accepted),
            new(3, U3, U1, AcceptedSwapStatuses.Completed)
        ];

        var existing = await db.AcceptedSwaps.Select(a => a.SwapId).ToListAsync(ct);
        var missing = swaps.Where(s => !existing.Contains(s.SwapId)).ToList();
        if (missing.Count == 0)
            return;

        db.AcceptedSwaps.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSessionsAsync(SessionsDbContext db, CancellationToken ct)
    {
        if (await db.Sessions.IgnoreQueryFilters().AnyAsync(ct))
            return;

        var inPerson = new Session(2, SessionFormatIds.InPerson, Utc(2026, 10, 22, 17), 90,
            null, "Чернівці, центр", U2);
        inPerson.AddSkill(2, 45);
        inPerson.AddSkill(3, 45);

        var online = new Session(2, SessionFormatIds.Online, Utc(2026, 10, 29, 17), 90,
            "https://meet.google.com/abc-defg-hij", null, U3);
        online.AddSkill(2, 45);
        online.AddSkill(3, 45);

        var completed = new Session(3, SessionFormatIds.Online, Utc(2026, 10, 5, 19), 120,
            "https://zoom.us/j/1234567890", null, U3);
        completed.AddSkill(4, 60);
        completed.AddSkill(1, 60);
        completed.Complete("Консультація з C# та огляд макета у Figma", 115, Utc(2026, 10, 5, 21));

        foreach (var session in new[] { inPerson, online, completed })
        {
            db.Sessions.Add(session);
            await db.SaveChangesAsync(ct);
        }
    }

    private static DateTime Utc(int year, int month, int day, int hour)
        => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
