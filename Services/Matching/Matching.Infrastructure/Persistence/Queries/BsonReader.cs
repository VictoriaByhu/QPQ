using MongoDB.Bson;
using Matching.Application.Common.Models;

namespace Matching.Infrastructure.Persistence.Queries;

/// <summary>Перетворює проєкції BsonDocument на DTO (читання без побудови доменних сутностей).</summary>
internal static class BsonReader
{
    public static int Int(this BsonDocument d, string name, int fallback = 0)
        => d.TryGetValue(name, out var v) && v.IsNumeric ? v.ToInt32() : fallback;

    public static DateTime Utc(this BsonValue v) => v.ToUniversalTime();

    public static IndexEntryDto ToIndexEntryDto(this BsonDocument d) => new(
        d["_id"].AsObjectId.ToString(),
        d.Int("userSkillId"), d["userId"].AsString, d.Int("skillId"), d["skillName"].AsString,
        d["type"].AsString, d.Int("level"), d["status"].AsString,
        d.TryGetValue("experienceYears", out var years) && years.IsNumeric ? years.ToInt32() : null,
        d["languages"].AsBsonArray.Select(l => new LanguageDto(l["code"].AsString, l["proficiency"].ToInt32())).ToList(),
        d["availability"].AsBsonArray.Select(a => new AvailabilityDto(
            a["dayOfWeek"].ToInt32(), a["start"].AsString, a["end"].AsString)).ToList(),
        d.Int("version"),
        d.TryGetValue("updatedAt", out var updated) && updated.IsValidDateTime ? updated.Utc() : null);

    private static MatchOfferDto ToOfferDto(BsonValue o) => new(
        o["fromUserId"].AsString, o["toUserId"].AsString, o["skillId"].ToInt32(),
        o["skillName"].AsString, o["indexRef"].AsObjectId.ToString());

    public static MatchDto ToMatchDto(this BsonDocument d) => new(
        d["_id"].AsObjectId.ToString(), d["kind"].AsString, d["status"].AsString, d.Int("score"),
        d["userIds"].AsBsonArray.Select(x => x.AsString).ToList(),
        ToOfferDto(d["offer"]),
        d.TryGetValue("counterOffer", out var counter) && counter.IsBsonDocument ? ToOfferDto(counter) : null,
        d["commonLanguages"].AsBsonArray.Select(x => x.AsString).ToList(),
        d["createdAt"].Utc(), d["expiresAt"].Utc(), d.Int("version"));

    public static SkillStatsDto ToSkillStatsDto(this BsonDocument d) => new(
        d["_id"].ToInt32(), d["skillName"].AsString, d.Int("offersCount"), d.Int("wantsCount"),
        d["topLanguages"].AsBsonArray.Select(l => new LanguageCountDto(l["code"].AsString, l["count"].ToInt32())).ToList(),
        d["updatedAt"].Utc());
}
