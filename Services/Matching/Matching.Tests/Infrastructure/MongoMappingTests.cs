using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Infrastructure.Persistence;
using static Matching.Tests.TestData;

namespace Matching.Tests.Infrastructure;

/// <summary>Мапінг BSON перевіряється без бази: серіалізація в документ і назад.</summary>
public class MongoMappingTests
{
    public MongoMappingTests() => MongoMappings.Register();

    private static BsonDocument ToDocument<T>(T value) => value.ToBsonDocument();

    private static T RoundTrip<T>(T value)
        => BsonSerializer.Deserialize<T>(value.ToBson());

    [Fact]
    public void Document_uses_camelCase_and_hides_private_dotnet_fields()
    {
        var doc = ToDocument(Entry(id: ObjectId.Parse("670000000000000000000d01")));

        Assert.Equal(
            ["_id", "createdAt", "updatedAt", "version", "userSkillId", "userId", "skillId", "skillName", "type",
             "level", "status", "experienceYears", "languages", "availability"],
            doc.Names.ToArray());
        Assert.Equal("Offer", doc["type"].AsString);
        Assert.Equal("uk", doc["languages"][0]["code"].AsString);
        Assert.DoesNotContain(doc.Names, n => n.StartsWith('_') && n != "_id");
    }

    [Fact]
    public void Want_document_has_no_experienceYears_field()
    {
        var doc = ToDocument(Entry(type: UserSkillType.Want));
        Assert.False(doc.Contains("experienceYears"));
    }

    [Fact]
    public void Entry_survives_a_round_trip_through_BSON_including_private_collections()
    {
        var original = Entry(languages: ["uk", "en"], slots: [(1, "18:00", "20:00"), (3, "10:00", "11:30")]);
        var copy = RoundTrip(original);

        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Languages, copy.Languages);               // порівняння Value Objects за значеннями
        Assert.Equal(original.Availability, copy.Availability);
        Assert.Equal(original.Type, copy.Type);
        Assert.Equal(DateTimeKind.Utc, copy.CreatedAt.Kind);
    }

    [Fact]
    public void Match_kind_is_stored_in_camelCase_and_counter_offer_only_for_mutual()
    {
        var oneWay = ToDocument(Match.OneWay(Offer(U1, U2), 70, ["uk"]));
        var mutual = ToDocument(Match.Mutual(Offer(U1, U2), Offer(U2, U1, 2), 90, ["uk"]));

        Assert.Equal("oneWay", oneWay["kind"].AsString);
        Assert.False(oneWay.Contains("counterOffer"));
        Assert.Equal("mutual", mutual["kind"].AsString);
        Assert.True(mutual["counterOffer"].IsBsonDocument);
        Assert.Equal("Suggested", mutual["status"].AsString);
        Assert.True(mutual["offer"]["indexRef"].IsObjectId);
        Assert.True(mutual["expiresAt"].IsValidDateTime);
    }

    [Fact]
    public void Match_survives_a_round_trip()
    {
        var original = Match.Mutual(Offer(U1, U2), Offer(U2, U1, 2), 90, ["uk", "en"]);
        var copy = RoundTrip(original);

        Assert.Equal(MatchKind.Mutual, copy.Kind);
        Assert.Equal(original.Offer, copy.Offer);
        Assert.Equal(original.CounterOffer, copy.CounterOffer);
        Assert.Equal(original.UserIds, copy.UserIds);
        Assert.Equal(original.CommonLanguages, copy.CommonLanguages);
    }

    [Fact]
    public void Legacy_document_without_version_field_is_read_as_version_zero()
    {
        var doc = ToDocument(Entry());
        doc.Remove("version");
        doc.Remove("createdAt");

        var entry = BsonSerializer.Deserialize<UserSkillIndexEntry>(doc);
        Assert.Equal(0, entry.Version);
    }

    [Fact]
    public void Unknown_extra_elements_do_not_break_reading()
    {
        var doc = ToDocument(Entry());
        doc["someFutureField"] = 1;
        Assert.NotNull(BsonSerializer.Deserialize<UserSkillIndexEntry>(doc));
    }
}
