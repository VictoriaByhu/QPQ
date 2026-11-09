using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Bll.Caching;
using Platform.Caching;
using Catalog.Bll.Dtos;
using Catalog.Domain.Exceptions;
using Catalog.Bll.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests;

/// <summary>Двохрівневий кеш: L1 → L2 → factory, інвалідація, версії груп, стійкість до збою Redis.</summary>
public sealed class TwoLevelCacheTests : IDisposable
{
    private static readonly CacheOptions Opt = new("Catalog");
    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 100, TrackStatistics = true });
    private readonly IDistributedCache _redis = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly CacheMetrics _metrics;

    public TwoLevelCacheTests() => _metrics = new CacheMetrics(_memory, Opt);

    private TwoLevelCache Create(IDistributedCache? distributed = null) =>
        new(_memory, distributed ?? _redis, _metrics, Opt, NullLogger<TwoLevelCache>.Instance);

    public void Dispose()
    {
        _metrics.Dispose();
        _memory.Dispose();
    }

    private record Item(int Id, string Name);

    [Fact]
    public async Task SecondRead_IsServedFromL1_WithoutCallingFactory()
    {
        var cache = Create();
        var calls = 0;
        Task<Item?> Factory(CancellationToken _) { calls++; return Task.FromResult<Item?>(new Item(1, "a")); }

        await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);
        var second = await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);

        Assert.Equal(1, calls);
        Assert.Equal("a", second!.Name);
        Assert.Equal((1, 1), _metrics.Totals(CacheMetrics.L1));       // 1 hit (друге читання) і 1 miss (перше)
    }

    [Fact]
    public async Task WhenL1IsEmpty_ValueIsTakenFromL2_AndL1IsRefilled()
    {
        var cache = Create();
        var calls = 0;
        Task<Item?> Factory(CancellationToken _) { calls++; return Task.FromResult<Item?>(new Item(1, "a")); }
        await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);

        _memory.Remove("k");                                          // як на іншій репліці: L1 порожній, Redis спільний
        var fromL2 = await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);

        Assert.Equal(1, calls);
        Assert.Equal(new Item(1, "a"), fromL2);
        Assert.True(_memory.TryGetValue("k", out Item? _));           // L1 заповнено на зворотному шляху
        Assert.Equal(1, _metrics.Totals(CacheMetrics.L2).Hits);
    }

    [Fact]
    public async Task Remove_InvalidatesBothLevels()
    {
        var cache = Create();
        var calls = 0;
        Task<Item?> Factory(CancellationToken _) { calls++; return Task.FromResult<Item?>(new Item(calls, "v" + calls)); }
        await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);

        await cache.RemoveAsync("k", "test");
        var after = await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory);

        Assert.Equal(2, calls);
        Assert.Equal("v2", after!.Name);
        Assert.Null(await _redis.GetAsync("missing"));
    }

    [Fact]
    public async Task NullFromFactory_IsNotCached()
    {
        var cache = Create();
        var calls = 0;
        Task<Item?> Factory(CancellationToken _) { calls++; return Task.FromResult<Item?>(null); }

        Assert.Null(await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory));
        Assert.Null(await cache.GetOrCreateAsync("k", CachePolicy.Entity, Factory));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task BumpVersion_ChangesGroupVersion()
    {
        var cache = Create();
        var before = await cache.GetVersionAsync("g");
        await cache.BumpVersionAsync("g", "test");
        var after = await cache.GetVersionAsync("g");
        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task RedisFailure_DoesNotBreakReads_AndL2IsSkippedAfterwards()
    {
        var broken = Substitute.For<IDistributedCache>();
        broken.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("redis down"));
        broken.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("redis down"));
        var cache = Create(broken);

        var value = await cache.GetOrCreateAsync("k", CachePolicy.Entity, _ => Task.FromResult<Item?>(new Item(1, "db")));

        Assert.Equal("db", value!.Name);                              // відповідь із «бази», а не виняток
        Assert.False(cache.IsDistributedCacheAvailable);              // L2 тимчасово пропускається
        Assert.Equal("db", (await cache.GetOrCreateAsync("k", CachePolicy.Entity, _ => Task.FromResult<Item?>(new Item(2, "x"))))!.Name);
    }

    [Fact]
    public void CachePolicy_RequiresL1ShorterThanL2()
    {
        Assert.Throws<ArgumentException>(() => new CachePolicy(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5)));
        Assert.True(CachePolicy.Entity.L1Absolute < CachePolicy.Entity.L2Absolute);
        Assert.True(CachePolicy.List.L1Absolute < CachePolicy.List.L2Absolute);
        Assert.True(CachePolicy.Reference.L1Absolute < CachePolicy.Reference.L2Absolute);
    }

    [Fact]
    public async Task L1_EvictsEntries_WhenSizeLimitIsReached()
    {
        var small = new MemoryCache(new MemoryCacheOptions { SizeLimit = 2, TrackStatistics = true });
        using var metrics = new CacheMetrics(small, Opt);
        var cache = new TwoLevelCache(small, _redis, metrics, Opt, NullLogger<TwoLevelCache>.Instance);

        for (var i = 0; i < 5; i++)
            await cache.GetOrCreateAsync($"k{i}", CachePolicy.Entity, _ => Task.FromResult<Item?>(new Item(i, "x")));
        small.Compact(0);                                             // примусове очищення черги витіснення

        var entries = small.GetCurrentStatistics()!.CurrentEntryCount;
        Assert.True(entries <= 2, $"L1 перевищив SizeLimit: {entries} записів");
    }
}

/// <summary>Декоратор SkillService: читання кешуються, будь-яка зміна синхронно інвалідує кеш.</summary>
public sealed class CachedSkillServiceTests : IDisposable
{
    private static readonly CacheOptions Opt = new("Catalog");
    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 1000 });
    private readonly CacheMetrics _metrics;
    private readonly ISkillService _inner = Substitute.For<ISkillService>();
    private readonly CachedSkillService _cached;

    public CachedSkillServiceTests()
    {
        _metrics = new CacheMetrics(_memory, Opt);
        var cache = new TwoLevelCache(_memory, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            _metrics, Opt, NullLogger<TwoLevelCache>.Instance);
        _cached = new CachedSkillService(_inner, cache);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _memory.Dispose();
    }

    private static SkillDto Dto(int id, string name) =>
        new(id, name, "slug", 1, "Cat", null, null, [], [], DateTime.UtcNow, null, "AAAA");

    [Fact]
    public async Task GetById_SecondCall_DoesNotReachDatabase()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Dto(1, "A"));

        await _cached.GetByIdAsync(1);
        await _cached.GetByIdAsync(1);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_InvalidatesCard_SoNextReadSeesNewData()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Dto(1, "old"), Dto(1, "new"));
        _inner.UpdateAsync(1, Arg.Any<UpdateSkillDto>(), Arg.Any<CancellationToken>()).Returns(Dto(1, "new"));

        Assert.Equal("old", (await _cached.GetByIdAsync(1)).Name);
        await _cached.UpdateAsync(1, new UpdateSkillDto());
        Assert.Equal("new", (await _cached.GetByIdAsync(1)).Name);
    }

    [Fact]
    public async Task Create_MovesListsToNewVersion_SoCachedPagesAreNotReused()
    {
        var empty = new PagedResult<SkillListItemDto>([], 0, 1, 20);
        var one = new PagedResult<SkillListItemDto>([new SkillListItemDto(1, "A", "a", 1, "C", [])], 1, 1, 20);
        _inner.GetListAsync(Arg.Any<SkillQuery>(), Arg.Any<CancellationToken>()).Returns(empty, one);
        _inner.CreateAsync(Arg.Any<CreateSkillDto>(), Arg.Any<CancellationToken>()).Returns(Dto(1, "A"));

        Assert.Equal(0, (await _cached.GetListAsync(new SkillQuery())).TotalCount);
        Assert.Equal(0, (await _cached.GetListAsync(new SkillQuery())).TotalCount);   // із кешу
        await _cached.CreateAsync(new CreateSkillDto());
        Assert.Equal(1, (await _cached.GetListAsync(new SkillQuery())).TotalCount);   // нова версія -> промах

        await _inner.Received(2).GetListAsync(Arg.Any<SkillQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotFound_IsNotCached()
    {
        _inner.GetByIdAsync(404, Arg.Any<CancellationToken>()).ThrowsAsync(new EntityNotFoundException("nope"));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _cached.GetByIdAsync(404));
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _cached.GetByIdAsync(404));

        await _inner.Received(2).GetByIdAsync(404, Arg.Any<CancellationToken>());
    }
}

/// <summary>Наскрізно через REST: повторне читання з кешу, а після PUT — оновлені дані (контрольні точки Лаб. 06).</summary>
public sealed class CachingApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public CachingApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Put_ThenGet_ReturnsUpdatedData_NotStaleCache()
    {
        var before = await _client.GetFromJsonAsync<SkillDto>("/api/skills/1", Json);
        await _client.GetFromJsonAsync<SkillDto>("/api/skills/1", Json);                // другий запит — з кешу

        var put = await _client.PutAsJsonAsync("/api/skills/1", new
        {
            categoryId = before!.CategoryId, name = "Програмування C# (оновлено)", slug = before.Slug,
            description = before.Description, iconUrl = before.IconUrl, rowVersion = before.RowVersion
        });
        put.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<SkillDto>("/api/skills/1", Json);
        Assert.Equal("Програмування C# (оновлено)", after!.Name);

        var list = await _client.GetFromJsonAsync<PagedResult<SkillListItemDto>>("/api/skills?pageSize=50", Json);
        Assert.Contains(list!.Items, i => i.Name == "Програмування C# (оновлено)");      // список теж інвалідовано
    }

    [Fact]
    public async Task Health_ReportsCacheStatistics()
    {
        await _client.GetAsync("/api/skills/1");
        await _client.GetAsync("/api/skills/1");

        var body = await _client.GetStringAsync("/health");
        Assert.Contains("Healthy", body);
    }
}
