using System.Diagnostics;
using Catalog.GrpcContracts;
using Grpc.Core;
using Matching.GrpcContracts;
using Platform.Aggregator.Models;
using UserSkills.GrpcContracts;

namespace Platform.Aggregator.Services;

public interface ISkillAggregationService
{
    Task<FullSkillDto?> GetFullSkillAsync(int skillId, CancellationToken ct);
}

/// <summary>Основний сервіс (довідник) недоступний — картку навички зібрати неможливо (контролер віддасть 503).</summary>
public sealed class UpstreamUnavailableException(string service, Exception inner)
    : Exception($"{service} is unavailable: {inner.Message}", inner)
{
    public string Service { get; } = service;
}

/// <summary>
/// Aggregator Pattern на gRPC: типізовані клієнти замість HttpClient, незалежні виклики — паралельно (Task.WhenAll)
/// через одне мультиплексоване HTTP/2-з'єднання на сервіс. Спершу перевіряється кеш у Redis, і лише при промаху
/// виконуються виклики. Недоступність другорядного сервісу (RpcException) не валить запит — відповідь
/// містить доступні дані та перелік попереджень. CancellationToken і deadline передаються в кожен виклик.
/// </summary>
public sealed class SkillAggregationService(
    CatalogService.CatalogServiceClient catalog,
    UserSkillsService.UserSkillsServiceClient userSkills,
    MatchingService.MatchingServiceClient matching,
    AggregateCache cache,
    ILogger<SkillAggregationService> logger) : ISkillAggregationService
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");
    private static readonly TimeSpan CallDeadline = TimeSpan.FromSeconds(3);

    public async Task<FullSkillDto?> GetFullSkillAsync(int skillId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("GetFullSkill");
        activity?.SetTag("skill.id", skillId);

        // Кеш перевіряється ДО паралельних gRPC-викликів.
        var key = AggregateCache.SkillKey(skillId);
        var cached = await cache.GetAsync<FullSkillDto>(key, ct);
        if (cached is not null)
        {
            activity?.SetTag("cache.hit", true);
            return cached;
        }
        activity?.SetTag("cache.hit", false);

        var warnings = new List<string>();

        // Без навички (довідник — власник SkillId) агрегувати нічого: її недоступність — це помилка запиту.
        var skillTask = CallAsync("catalog-api", skillId, async () =>
        {
            try
            {
                var reply = await catalog.GetSkillAsync(new GetSkillRequest { Id = skillId }, Options(ct)).ResponseAsync;
                return ToDto(reply);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return null;                                              // навички немає -> 404 для клієнта
            }
        }, critical: true);

        var userSkillsTask = SafeAsync(() => CallAsync("userskills-api", skillId, async () =>
        {
            var reply = await userSkills.ListBySkillAsync(
                new ListBySkillRequest { SkillId = skillId, Limit = 20 }, Options(ct)).ResponseAsync;
            return (IReadOnlyList<UserSkillSummaryDto>?)reply.UserSkills.Select(ToDto).ToList();
        }), "userskills-api", warnings);

        var statsTask = SafeAsync(() => CallAsync("matching-api", skillId, async () =>
        {
            try
            {
                var reply = await matching.GetSkillStatsAsync(new GetSkillStatsRequest { SkillId = skillId }, Options(ct)).ResponseAsync;
                return ToDto(reply);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return null;                                              // статистики ще немає — не помилка
            }
        }), "matching-api", warnings);

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

        var result = new FullSkillDto(skill, records, stats, warnings);

        // Часткову відповідь не кешуємо: інакше «неповна» картка прожила б увесь TTL після відновлення сервісу.
        if (warnings.Count == 0)
            await cache.SetAsync(key, result, ct);
        return result;
    }

    private static CallOptions Options(CancellationToken ct) =>
        new(deadline: DateTime.UtcNow.Add(CallDeadline), cancellationToken: ct);

    private async Task<T?> CallAsync<T>(string service, int skillId, Func<Task<T?>> call, bool critical = false)
    {
        logger.LogInformation("Calling {Service} over gRPC for skill {SkillId}", service, skillId);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) when (critical && IsUnavailable(ex))
        {
            logger.LogWarning(ex, "{Service} is unavailable", service);
            throw new UpstreamUnavailableException(service, ex);
        }
    }

    private async Task<T?> SafeAsync<T>(Func<Task<T?>> call, string service, List<string> warnings)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            logger.LogWarning(ex, "{Service} is unavailable", service);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return default;
        }
    }

    /// <summary>Часткова відмова: Unavailable / DeadlineExceeded / Internal від gRPC, а також розрив з'єднання, таймаут і circuit breaker.</summary>
    private static bool IsUnavailable(Exception ex) =>
        ex is RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Internal or StatusCode.Unknown }
            or HttpRequestException or TimeoutException
            or Polly.CircuitBreaker.BrokenCircuitException or Polly.Timeout.TimeoutRejectedException;

    // ── gRPC-повідомлення -> агреговані DTO Лаб. 05 (JSON-контракт для клієнтів не змінився) ──
    private static SkillDto ToDto(SkillReply r) => new(
        r.Id, r.Name, r.Slug, r.CategoryId, r.CategoryName,
        r.HasDescription ? r.Description : null, r.HasIconUrl ? r.IconUrl : null,
        r.Levels.Select(l => new SkillLevelDto((byte)l.Level, l.Title, l.HasDescription ? l.Description : null)).ToList(),
        r.Tags.ToList());

    private static UserSkillSummaryDto ToDto(UserSkillSummary r) => new(
        r.Id, Guid.Parse(r.UserId), r.SkillId, r.SkillName, r.Type, (byte)r.Level, r.Status, r.CreatedAt.ToDateTime());

    private static SkillStatsDto ToDto(SkillStatsReply r) => new(
        r.SkillId, r.SkillName, r.OffersCount, r.WantsCount,
        r.TopLanguages.Select(l => new LanguageCountDto(l.Code, l.Count)).ToList(), r.UpdatedAt.ToDateTime());
}
