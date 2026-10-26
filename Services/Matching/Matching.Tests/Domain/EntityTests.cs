using MongoDB.Bson;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.Services;
using Matching.Domain.ValueObjects;
using static Matching.Tests.TestData;

namespace Matching.Tests.Domain;

public class EntityTests
{
    [Fact]
    public void Level_out_of_range_throws_DomainException_without_database()
        => Assert.Throws<DomainException>(() => Entry(level: 10));

    [Fact]
    public void Want_cannot_have_experience_years()
        => Assert.Throws<DomainException>(() => new UserSkillIndexEntry(
            1, U1, 1, "C#", UserSkillType.Want, 2, UserSkillStatus.Active, 3,
            [new Language("uk", 5)], []));

    [Fact]
    public void Invalid_user_id_is_rejected()
        => Assert.Throws<DomainException>(() => Entry(userId: "not-a-guid"));

    [Fact]
    public void Duplicate_language_codes_are_rejected()
        => Assert.Throws<DomainException>(() => new UserSkillIndexEntry(
            1, U1, 1, "C#", UserSkillType.Offer, 3, UserSkillStatus.Active, 1,
            [new Language("uk", 5), new Language("UK", 3)], []));

    [Fact]
    public void Update_changes_state_and_touches_UpdatedAt_but_keeps_identity()
    {
        var entry = Entry();
        var type = entry.Type;
        entry.Update("Renamed", 2, UserSkillStatus.Paused, 7, [new Language("en", 4)], []);

        Assert.Equal("Renamed", entry.SkillName);
        Assert.Equal(UserSkillStatus.Paused, entry.Status);
        Assert.Equal(type, entry.Type);
        Assert.Single(entry.Languages);
        Assert.NotNull(entry.UpdatedAt);
    }

    [Fact]
    public void Collections_are_exposed_read_only()
    {
        var entry = Entry();
        Assert.IsAssignableFrom<IReadOnlyList<Language>>(entry.Languages);
        Assert.False(entry.Languages is List<Language>);
    }

    [Fact]
    public void Value_objects_compare_by_value()
    {
        Assert.Equal(new Language("uk", 5), new Language("UK", 5));
        Assert.Equal(new Language("uk", 5).GetHashCode(), new Language("uk", 5).GetHashCode());
        Assert.NotEqual(new Language("uk", 5), new Language("uk", 4));
        Assert.True(new AvailabilitySlot(1, "10:00", "11:00") == new AvailabilitySlot(1, "10:00", "11:00"));
    }

    [Theory]
    [InlineData("25:00", "26:00")]
    [InlineData("12:00", "10:00")]
    [InlineData("9:00", "10:00")]
    public void Availability_slot_validates_time(string from, string to)
        => Assert.Throws<DomainException>(() => new AvailabilitySlot(1, from, to));

    [Fact]
    public void Availability_slots_overlap_only_on_same_day()
    {
        var a = new AvailabilitySlot(1, "10:00", "12:00");
        Assert.True(a.Overlaps(new AvailabilitySlot(1, "11:00", "13:00")));
        Assert.False(a.Overlaps(new AvailabilitySlot(1, "12:00", "13:00")));
        Assert.False(a.Overlaps(new AvailabilitySlot(2, "10:00", "12:00")));
    }

    [Fact]
    public void Match_offer_rejects_same_user()
        => Assert.Throws<DomainException>(() => Offer(U1, U1));

    [Fact]
    public void Mutual_match_requires_reverse_counter_offer()
    {
        Assert.Throws<DomainException>(() => Match.Mutual(Offer(U1, U2), Offer(U1, U3, 2), 80, ["uk"]));
        var match = Match.Mutual(Offer(U1, U2), Offer(U2, U1, 2), 80, ["uk"]);
        Assert.Equal(MatchKind.Mutual, match.Kind);
        Assert.Equal(2, match.UserIds.Count);
    }

    [Fact]
    public void Match_status_transitions_follow_the_rules()
    {
        var match = Match.OneWay(Offer(U1, U2), 70, ["uk"]);
        match.ChangeStatus(MatchStatus.Viewed);
        match.ChangeStatus(MatchStatus.Dismissed);
        Assert.Throws<DomainException>(() => match.ChangeStatus(MatchStatus.Viewed));
        Assert.Throws<DomainException>(() => Match.OneWay(Offer(U1, U2), 70, ["uk"]).ChangeStatus(MatchStatus.Suggested));
    }

    [Fact]
    public void OneWay_match_can_be_upgraded_to_mutual_once()
    {
        var match = Match.OneWay(Offer(U1, U2), 70, ["uk"]);
        match.UpgradeToMutual(Offer(U2, U1, 2), 95);
        Assert.Equal(MatchKind.Mutual, match.Kind);
        Assert.Equal(95, match.Score);
        Assert.Throws<DomainException>(() => match.UpgradeToMutual(Offer(U2, U1, 2), 95));
    }

    [Fact]
    public void Score_is_capped_at_100_and_requires_compatibility()
    {
        var want = Entry(1, U2, 1, UserSkillType.Want, level: 2, languages: ["uk", "en"]);
        var offer = Entry(2, U1, 1, UserSkillType.Offer, level: 4, languages: ["uk", "en"]);

        Assert.True(MatchmakingPolicy.IsCompatible(want, offer));
        Assert.Equal(100, MatchmakingPolicy.Score(want, offer, mutual: true));
        Assert.Equal(75, MatchmakingPolicy.Score(want, offer, mutual: false));

        var weaker = Entry(3, U1, 1, UserSkillType.Offer, level: 1);
        Assert.False(MatchmakingPolicy.IsCompatible(want, weaker));                       // рівень нижчий за бажаний
        var otherLanguage = Entry(4, U1, 1, UserSkillType.Offer, level: 4, languages: ["pl"]);
        Assert.False(MatchmakingPolicy.IsCompatible(want, otherLanguage));               // немає спільної мови
        var paused = Entry(5, U1, 1, UserSkillType.Offer, level: 4, status: UserSkillStatus.Paused);
        Assert.False(MatchmakingPolicy.IsCompatible(want, paused));
    }
}
