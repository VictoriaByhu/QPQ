using System.Diagnostics.Metrics;

namespace Platform.Aggregator.Caching;

public sealed class AggregatorCacheMetrics
{
    public const string MeterName = "Aggregator.Cache";

    private const string LevelTag = "cache.level";
    private const string Level = "L2";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;

    public AggregatorCacheMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits");
        _misses = meter.CreateCounter<long>("cache.misses");
    }

    public void Hit() => _hits.Add(1, new KeyValuePair<string, object?>(LevelTag, Level));

    public void Miss() => _misses.Add(1, new KeyValuePair<string, object?>(LevelTag, Level));
}
