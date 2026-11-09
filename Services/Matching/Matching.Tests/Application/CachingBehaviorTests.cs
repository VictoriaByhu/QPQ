using Matching.Application.Caching;
using Matching.Application.SkillStats.Commands.RebuildSkillStats;
using Matching.Application.SkillStats.Queries.GetSkillStats;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Caching;

namespace Matching.Tests.Application;

/// <summary>CachingBehavior: cache-aside для запитів (L1+L2) і інвалідація групи командою.</summary>
public sealed class CachingBehaviorTests : IDisposable
{
    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 1000 });
    private readonly CacheMetrics _metrics;
    private readonly TwoLevelCache _cache;

    public CachingBehaviorTests()
    {
        var options = new CacheOptions("Matching");
        _metrics = new CacheMetrics(_memory, options);
        IDistributedCache redis = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cache = new TwoLevelCache(_memory, redis, _metrics, options, NullLogger<TwoLevelCache>.Instance);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _memory.Dispose();
    }

    [Fact]
    public async Task Query_IsLoadedOnce_ThenServedFromCache()
    {
        var behavior = new CachingBehavior<GetSkillStatsQuery, string>(_cache);
        var calls = 0;
        Task<string> Next(CancellationToken _) { calls++; return Task.FromResult("stats"); }

        var first = await behavior.Handle(new GetSkillStatsQuery(1), Next, default);
        var second = await behavior.Handle(new GetSkillStatsQuery(1), Next, default);

        Assert.Equal("stats", first);
        Assert.Equal("stats", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DifferentParameters_UseDifferentKeys()
    {
        var behavior = new CachingBehavior<GetSkillStatsQuery, string>(_cache);
        var calls = 0;
        Task<string> Next(CancellationToken _) { calls++; return Task.FromResult("x"); }

        await behavior.Handle(new GetSkillStatsQuery(1), Next, default);
        await behavior.Handle(new GetSkillStatsQuery(2), Next, default);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Command_InvalidatesGroup_SoQueryReloads()
    {
        var query = new CachingBehavior<GetSkillStatsQuery, string>(_cache);
        var command = new CachingBehavior<RebuildSkillStatsCommand, int>(_cache);
        var calls = 0;
        Task<string> Next(CancellationToken _) { calls++; return Task.FromResult($"v{calls}"); }

        await query.Handle(new GetSkillStatsQuery(1), Next, default);
        await command.Handle(new RebuildSkillStatsCommand(), _ => Task.FromResult(6), default);
        var after = await query.Handle(new GetSkillStatsQuery(1), Next, default);

        Assert.Equal(2, calls);
        Assert.Equal("v2", after);
    }

    [Fact]
    public async Task HandlerException_IsNotCached()
    {
        var behavior = new CachingBehavior<GetSkillStatsQuery, string>(_cache);
        Task<string> Fail(CancellationToken _) => throw new InvalidOperationException("not found");

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new GetSkillStatsQuery(9), Fail, default));
        var ok = await behavior.Handle(new GetSkillStatsQuery(9), _ => Task.FromResult("fresh"), default);

        Assert.Equal("fresh", ok);
    }
}
