using System.Net;
using System.Net.Http.Json;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

/// <summary>Базова логіка: 404 -> null, інші помилки -> HttpRequestException (обробляє Aggregator).</summary>
public abstract class ServiceClientBase(HttpClient http)
{
    protected async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
}

public sealed class CatalogClient(HttpClient http) : ServiceClientBase(http)
{
    public Task<SkillDto?> GetSkillAsync(int skillId, CancellationToken ct)
        => GetOrNullAsync<SkillDto>($"/api/skills/{skillId}", ct);
}

public sealed class UserSkillsClient(HttpClient http) : ServiceClientBase(http)
{
    public async Task<IReadOnlyList<UserSkillSummaryDto>> GetBySkillAsync(int skillId, int limit, CancellationToken ct)
        => await GetOrNullAsync<List<UserSkillSummaryDto>>($"/api/user-skills/by-skill/{skillId}?limit={limit}", ct) ?? [];
}

public sealed class MatchingClient(HttpClient http) : ServiceClientBase(http)
{
    public Task<SkillStatsDto?> GetStatsAsync(int skillId, CancellationToken ct)
        => GetOrNullAsync<SkillStatsDto>($"/api/skill-stats/{skillId}", ct);
}
