using Catalog.Bll.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Api.Caching;

/// <summary>
/// Статистика кешу у health check сервісу (/health): hits/misses і hit ratio за рівнями.
/// Недоступний Redis не робить сервіс нездоровим — він працює далі лише з L1 і базою, тому статус Degraded.
/// </summary>
public sealed class CacheHealthCheck(CacheMetrics metrics, ITwoLevelCache cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var (l1Hits, l1Misses) = metrics.Totals(CacheMetrics.L1);
        var (l2Hits, l2Misses) = metrics.Totals(CacheMetrics.L2);
        var data = new Dictionary<string, object>
        {
            ["l1.hits"] = l1Hits, ["l1.misses"] = l1Misses, ["l1.hitRatio"] = Math.Round(metrics.HitRatio(CacheMetrics.L1), 3),
            ["l2.hits"] = l2Hits, ["l2.misses"] = l2Misses, ["l2.hitRatio"] = Math.Round(metrics.HitRatio(CacheMetrics.L2), 3),
            ["l2.available"] = cache.IsDistributedCacheAvailable
        };

        return Task.FromResult(cache.IsDistributedCacheAvailable
            ? HealthCheckResult.Healthy("Cache statistics", data)
            : HealthCheckResult.Degraded("Redis is unavailable: serving from L1 and the database", data: data));
    }
}
