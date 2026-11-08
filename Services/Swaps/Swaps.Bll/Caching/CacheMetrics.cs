using System.Diagnostics.Metrics;

namespace Swaps.Bll.Caching;

public sealed class CacheMetrics
{
    public const string MeterName = "Swaps.Cache";

    private const string LevelTag = "cache.level";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;

    public CacheMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits");
        _misses = meter.CreateCounter<long>("cache.misses");
        _invalidations = meter.CreateCounter<long>("cache.invalidations");
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>(LevelTag, level));

    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>(LevelTag, level));

    public void Invalidated() => _invalidations.Add(1);
}
