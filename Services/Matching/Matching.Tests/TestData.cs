using MongoDB.Bson;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.ValueObjects;

namespace Matching.Tests;

internal static class TestData
{
    public const string U1 = "3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01";
    public const string U2 = "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02";
    public const string U3 = "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03";

    public static UserSkillIndexEntry Entry(
        int userSkillId = 1, string userId = U1, int skillId = 1, UserSkillType type = UserSkillType.Offer,
        int level = 4, UserSkillStatus status = UserSkillStatus.Active, int? years = 5,
        string[]? languages = null, (int Day, string From, string To)[]? slots = null, ObjectId? id = null)
        => new(userSkillId, userId, skillId, $"Skill {skillId}", type, level, status,
            type == UserSkillType.Offer ? years : null,
            (languages ?? ["uk"]).Select(c => new Language(c, 5)),
            (slots ?? [(1, "18:00", "20:00")]).Select(s => new AvailabilitySlot(s.Day, s.From, s.To)),
            id);

    public static MatchOffer Offer(string from, string to, int skillId = 1, ObjectId? indexRef = null)
        => new(from, to, skillId, $"Skill {skillId}", indexRef ?? ObjectId.GenerateNewId());
}
