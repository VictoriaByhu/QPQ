using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

/// <summary>
/// Власні метрики кешу (Meter "Catalog.Cache", підключений у ServiceDefaults): лічильники попадань/промахів
/// окремо для рівнів L1 (пам'ять) і L2 (Redis), інвалідацій і витіснень, гістограма тривалості операцій,
/// розмір L1 і hit ratio за рівнями. У Aspire Dashboard вони з'являються на вкладці Metrics.
/// </summary>
public sealed class CacheMetrics : IDisposable
{
    public const string MeterName = "Catalog.Cache";
    public const string L1 = "L1";
    public const string L2 = "L2";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _hits, _misses, _invalidations, _evictions;
    private readonly Histogram<double> _duration;
    private readonly long[] _hitCounts = new long[2], _missCounts = new long[2];   // для hit ratio й health check

    public CacheMetrics(IMemoryCache memory)
    {
        _hits = _meter.CreateCounter<long>("cache.hits", description: "Cache hits by level");
        _misses = _meter.CreateCounter<long>("cache.misses", description: "Cache misses by level");
        _invalidations = _meter.CreateCounter<long>("cache.invalidations", description: "Cache invalidations by reason");
        _evictions = _meter.CreateCounter<long>("cache.evictions", description: "L1 entries evicted by reason");
        _duration = _meter.CreateHistogram<double>("cache.operation.duration", unit: "ms", description: "Cache operation duration");

        _meter.CreateObservableGauge("cache.l1.entries",
            () => (memory as MemoryCache)?.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
            description: "Entries in the in-memory cache");
        _meter.CreateObservableGauge("cache.l1.size",
            () => (memory as MemoryCache)?.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0,
            description: "Estimated size of the in-memory cache (SizeLimit units)");
        _meter.CreateObservableGauge("cache.hit_ratio", () => new[]
        {
            new Measurement<double>(HitRatio(L1), new KeyValuePair<string, object?>("cache.level", L1)),
            new Measurement<double>(HitRatio(L2), new KeyValuePair<string, object?>("cache.level", L2))
        }, description: "Hit ratio by level (0..1)");
    }

    public void Hit(string level)
    {
        Interlocked.Increment(ref _hitCounts[Index(level)]);
        _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    }

    public void Miss(string level)
    {
        Interlocked.Increment(ref _missCounts[Index(level)]);
        _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    }

    public void Invalidated(string reason) =>
        _invalidations.Add(1, new KeyValuePair<string, object?>("cache.reason", reason));

    public void Evicted(EvictionReason reason) =>
        _evictions.Add(1, new KeyValuePair<string, object?>("cache.reason", reason.ToString()));

    public void Duration(string level, string operation, double milliseconds) =>
        _duration.Record(milliseconds,
            new KeyValuePair<string, object?>("cache.level", level),
            new KeyValuePair<string, object?>("cache.operation", operation));

    /// <summary>Частка попадань рівня (0..1); 0, поки не було жодного звернення.</summary>
    public double HitRatio(string level)
    {
        var hits = Interlocked.Read(ref _hitCounts[Index(level)]);
        var total = hits + Interlocked.Read(ref _missCounts[Index(level)]);
        return total == 0 ? 0 : (double)hits / total;
    }

    public (long Hits, long Misses) Totals(string level) =>
        (Interlocked.Read(ref _hitCounts[Index(level)]), Interlocked.Read(ref _missCounts[Index(level)]));

    private static int Index(string level) => level == L1 ? 0 : 1;

    public void Dispose() => _meter.Dispose();
}
