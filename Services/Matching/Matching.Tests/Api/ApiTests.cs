using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Matching.Tests.Infrastructure;
using static Matching.Tests.TestData;

namespace Matching.Tests.Api;

[Collection(MongoCollection.Name)]
public class ApiTests : IDisposable
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApiTests(MongoFixture mongo)
    {
        _factory = new ApiFactory(mongo);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object NewEntry(int userSkillId = 100, int level = 4, string type = "Offer", int? years = 3) => new
    {
        userSkillId, userId = U1, skillId = 1, skillName = "Програмування C#", type, level,
        status = "Active", experienceYears = years,
        languages = new[] { new { code = "uk", proficiency = 5 } },
        availability = new[] { new { dayOfWeek = 1, start = "18:00", end = "20:00" } }
    };

    [MongoFact]
    public async Task Post_returns_201_with_location_and_plain_object_id()
    {
        var response = await _client.PostAsJsonAsync("/api/index-entries", NewEntry());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<string>())!;
        Assert.Matches("^[0-9a-f]{24}$", id);                                    // рядок ObjectId, а не сирий BSON
        Assert.EndsWith($"/api/index-entries/{id}", response.Headers.Location!.ToString());

        var get = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("\"0\"", get.Headers.ETag!.Tag);
    }

    [MongoFact]
    public async Task Rating_out_of_range_returns_400_problem_details_before_the_handler()
    {
        var response = await _client.PostAsJsonAsync("/api/index-entries", NewEntry(userSkillId: 101, level: 10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("Level", out _));
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/index-entries?skillId=1&type=Offer&pageSize=100");
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), i => i.GetProperty("userSkillId").GetInt32() == 101);
    }

    [MongoFact]
    public async Task Duplicate_user_skill_id_is_rejected_without_500()
    {
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/index-entries", NewEntry(200))).StatusCode);
        var second = await _client.PostAsJsonAsync("/api/index-entries", NewEntry(200));

        Assert.True(second.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
    }

    [MongoFact]
    public async Task Missing_document_returns_404_and_malformed_id_returns_400()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/index-entries/670000000000000000000fff")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/index-entries/abc")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/index-entries/670000000000000000000fff")).StatusCode);
    }

    [MongoFact]
    public async Task Put_uses_if_match_for_optimistic_concurrency()
    {
        var id = (await (await _client.PostAsJsonAsync("/api/index-entries", NewEntry(300))).Content.ReadFromJsonAsync<string>())!;
        var update = new
        {
            skillName = "Renamed", level = 3, status = "Paused", experienceYears = 4,
            languages = new[] { new { code = "en", proficiency = 4 } },
            availability = Array.Empty<object>()
        };

        // Без If-Match -> 428.
        var noHeader = await _client.PutAsJsonAsync($"/api/index-entries/{id}", update);
        Assert.Equal((HttpStatusCode)428, noHeader.StatusCode);

        // Очікувана версія 0 -> 204 і нова версія в ETag.
        var ok = await Put(id, update, "\"0\"");
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.Equal("\"1\"", ok.Headers.ETag!.Tag);

        // Повторне оновлення зі старою версією -> 409.
        Assert.Equal(HttpStatusCode.Conflict, (await Put(id, update, "\"0\"")).StatusCode);

        var current = await _client.GetFromJsonAsync<JsonElement>($"/api/index-entries/{id}");
        Assert.Equal("Renamed", current.GetProperty("skillName").GetString());
        Assert.Equal(1, current.GetProperty("version").GetInt32());
    }

    private Task<HttpResponseMessage> Put(string id, object body, string ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/index-entries/{id}") { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return _client.SendAsync(request);
    }

    [MongoFact]
    public async Task Delete_returns_204_then_404()
    {
        var id = (await (await _client.PostAsJsonAsync("/api/index-entries", NewEntry(400))).Content.ReadFromJsonAsync<string>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/index-entries/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/index-entries/{id}")).StatusCode);
    }

    [MongoFact]
    public async Task Seeded_matches_are_listed_for_a_participant_only()
    {
        var page = await _client.GetFromJsonAsync<JsonElement>($"/api/matches?userId={U1}");
        var items = page.GetProperty("items").EnumerateArray().ToList();

        Assert.Single(items);                                                    // відхилена пара прихована
        Assert.Contains(U1, items[0].GetProperty("userIds").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal("mutual", items[0].GetProperty("kind").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/matches?userId=nope")).StatusCode);
    }

    [MongoFact]
    public async Task Generate_is_idempotent_and_status_changes_follow_the_rules()
    {
        // U4 хоче навичку 1 (Archived) — нових пар немає; U2 вже має seed-пари -> усе пропускається.
        var first = await _client.PostAsJsonAsync("/api/matches/generate", new { userId = U2 });
        var second = await _client.PostAsJsonAsync("/api/matches/generate", new { userId = U2 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var a = await first.Content.ReadFromJsonAsync<JsonElement>();
        var b = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, b.GetProperty("created").GetInt32());
        Assert.True(a.GetProperty("created").GetInt32() >= 0);

        // Зміна статусу з If-Match.
        const string id = "670000000000000000000e01";
        var match = await _client.GetAsync($"/api/matches/{id}");
        var etag = match.Headers.ETag!.Tag;

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/matches/{id}/status") { Content = JsonContent.Create(new { status = "Viewed" }) };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(request)).StatusCode);

        var stale = new HttpRequestMessage(HttpMethod.Put, $"/api/matches/{id}/status") { Content = JsonContent.Create(new { status = "Dismissed" }) };
        stale.Headers.TryAddWithoutValidation("If-Match", etag);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(stale)).StatusCode);

        var back = new HttpRequestMessage(HttpMethod.Put, $"/api/matches/{id}/status") { Content = JsonContent.Create(new { status = "Suggested" }) };
        back.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(back)).StatusCode);
    }

    [MongoFact]
    public async Task Skill_stats_endpoints_work()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync("/api/skill-stats/rebuild", null)).StatusCode);
        var stats = await _client.GetFromJsonAsync<JsonElement>("/api/skill-stats/1");
        Assert.Equal(1, stats.GetProperty("offersCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/skill-stats/999")).StatusCode);
    }

    [MongoFact]
    public async Task Health_check_reports_mongodb()
        => Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
}
