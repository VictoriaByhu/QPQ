using MongoDB.Bson;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.ValueObjects;

namespace Matching.Infrastructure.Seeding;

/// <summary>
/// Ті самі дані, що у db/p3/matching.seed.js (користувачі U1-U4 та навички 1-6 однакові в усіх базах QPQ,
/// ObjectId збігаються, тому сідінг із скрипта й із застосунку взаємозамінні).
/// </summary>
internal static class SeedData
{
    public const string U1 = "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01";
    public const string U2 = "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02";
    public const string U3 = "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03";
    public const string U4 = "e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04";

    private static readonly DateTime IndexTime = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<int, string> Names = new()
    {
        [1] = "Програмування C#", [2] = "Англійська розмовна", [3] = "Гра на гітарі",
        [4] = "UI-дизайн", [5] = "Фотографія", [6] = "Приготування випічки"
    };

    private static ObjectId Oid(string suffix) => ObjectId.Parse("670000000000000000000" + suffix);

    private static Language L(string code, int p) => new(code, p);
    private static AvailabilitySlot S(int day, string from, string to) => new(day, from, to);

    private static UserSkillIndexEntry Entry(
        string idSuffix, int userSkillId, string userId, int skillId, UserSkillType type, int level,
        UserSkillStatus status, int? years, Language[] languages, AvailabilitySlot[] slots)
        => new(userSkillId, userId, skillId, Names[skillId], type, level, status, years, languages, slots,
            Oid(idSuffix), IndexTime);

    public static IReadOnlyList<UserSkillIndexEntry> IndexEntries() =>
    [
        Entry("d01", 1, U1, 1, UserSkillType.Offer, 4, UserSkillStatus.Active, 5, [L("uk", 5), L("en", 3)], [S(1, "18:00", "20:00"), S(3, "18:00", "20:00")]),
        Entry("d02", 2, U1, 2, UserSkillType.Want, 2, UserSkillStatus.Active, null, [L("uk", 5)], [S(6, "10:00", "12:00")]),
        Entry("d03", 3, U2, 2, UserSkillType.Offer, 5, UserSkillStatus.Active, 8, [L("uk", 5), L("en", 5), L("pl", 3)], [S(2, "17:00", "19:00"), S(4, "17:00", "19:00")]),
        Entry("d04", 4, U2, 1, UserSkillType.Want, 2, UserSkillStatus.Active, null, [L("uk", 5)], [S(1, "18:00", "20:00")]),
        Entry("d05", 5, U2, 3, UserSkillType.Want, 1, UserSkillStatus.Active, null, [L("uk", 5)], [S(4, "17:00", "19:00")]),
        Entry("d06", 6, U3, 3, UserSkillType.Offer, 4, UserSkillStatus.Active, 6, [L("uk", 5)], [S(5, "16:00", "18:00")]),
        Entry("d07", 7, U3, 4, UserSkillType.Offer, 3, UserSkillStatus.Active, 3, [L("uk", 5), L("en", 4)], [S(7, "12:00", "14:00")]),
        Entry("d08", 8, U3, 1, UserSkillType.Want, 2, UserSkillStatus.Paused, null, [L("uk", 5)], [S(3, "19:00", "21:00")]),
        Entry("d09", 9, U4, 5, UserSkillType.Offer, 4, UserSkillStatus.Active, 4, [L("uk", 5), L("en", 3)], [S(6, "12:00", "14:00")]),
        Entry("d0a", 10, U4, 1, UserSkillType.Want, 1, UserSkillStatus.Archived, null, [L("uk", 5)], [])
    ];

    private static MatchOffer Offer(string from, string to, int skillId, string indexSuffix)
        => new(from, to, skillId, Names[skillId], Oid(indexSuffix));

    public static IReadOnlyList<Match> Matches()
    {
        var expires = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        return
        [
            Match.Mutual(Offer(U1, U2, 1, "d01"), Offer(U2, U1, 2, "d03"), 95, ["uk"],
                MatchStatus.Suggested, expires, Oid("e01"), new DateTime(2026, 10, 5, 12, 5, 0, DateTimeKind.Utc)),
            Match.OneWay(Offer(U3, U2, 3, "d06"), 70, ["uk"],
                MatchStatus.Viewed, expires, Oid("e02"), new DateTime(2026, 10, 5, 12, 6, 0, DateTimeKind.Utc)),
            Match.OneWay(Offer(U1, U3, 1, "d01"), 55, ["uk"],
                MatchStatus.Dismissed, expires, Oid("e03"), new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc))
        ];
    }
}
