using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public interface ISkillAggregationService
{
    Task<FullSkillDto?> GetFullSkillAsync(int skillId, CancellationToken ct);
}

/// <summary>
/// Aggregator Pattern: незалежні виклики виконуються паралельно (Task.WhenAll), недоступність
/// другорядного сервісу не валить запит — відповідь містить доступні дані та перелік попереджень.
/// </summary>
public sealed class SkillAggregationService(
    CatalogClient catalog, UserSkillsClient userSkills, MatchingClient matching,
    ILogger<SkillAggregationService> logger) : ISkillAggregationService
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");

    public async Task<FullSkillDto?> GetFullSkillAsync(int skillId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetFullSkill");
        activity?.SetTag("skill.id", skillId);
        var warnings = new List<string>();

        // Без навички (довідник — власник SkillId) агрегувати нічого: її недоступність — це помилка запиту.
        var skillTask = CallAsync("catalog-api", skillId, () => catalog.GetSkillAsync(skillId, ct));
        var userSkillsTask = SafeAsync(() => userSkills.GetBySkillAsync(skillId, 20, ct), "userskills-api", skillId, warnings);
        var statsTask = SafeAsync(() => matching.GetStatsAsync(skillId, ct), "matching-api", skillId, warnings);

        await Task.WhenAll(skillTask, userSkillsTask, statsTask);        // паралельно, без .Result / WaitAll

        var skill = await skillTask;
        if (skill is null) return null;

        // Узгодженість: записи користувачів і статистика мають належати саме цій навичці.
        var records = (await userSkillsTask ?? []).Where(u => u.SkillId == skillId).ToList();
        var stats = await statsTask;
        if (stats is not null && stats.SkillId != skillId)
        {
            warnings.Add("matching-api returned stats for a different skill and they were ignored");
            stats = null;
        }

        return new FullSkillDto(skill, records, stats, warnings);
    }

    private async Task<T?> CallAsync<T>(string service, int skillId, Func<Task<T?>> call)
    {
        logger.LogInformation("Calling {Service} for skill {SkillId}", service, skillId);
        var sw = Stopwatch.StartNew();
        var result = await call();
        logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
        return result;
    }

    private async Task<T?> SafeAsync<T>(Func<Task<T?>> call, string service, int skillId, List<string> warnings)
    {
        try
        {
            return await CallAsync(service, skillId, call);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException
                                       or Polly.CircuitBreaker.BrokenCircuitException
                                       or Polly.Timeout.TimeoutRejectedException)
        {
            logger.LogWarning(ex, "{Service} is unavailable", service);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return default;
        }
    }
}
