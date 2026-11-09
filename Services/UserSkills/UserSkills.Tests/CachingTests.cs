using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using Platform.Caching;
using UserSkills.Bll.Caching;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;

namespace UserSkills.Tests;

/// <summary>Декоратори кешу UserSkills: довідники читаються з кешу, вибірка за навичкою інвалідується записами.</summary>
public sealed class CachingTests : IDisposable
{
    private static readonly System.Diagnostics.Metrics.IMeterFactory Meters =
        new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddMetrics().BuildServiceProvider()
            .GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>();
    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 1000 });
    private readonly CacheMetrics _metrics;
    private readonly TwoLevelCache _cache;

    public CachingTests()
    {
        var options = new CacheOptions("UserSkills");
        _metrics = new CacheMetrics(_memory, options, Meters);
        IDistributedCache redis = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cache = new TwoLevelCache(_memory, redis, _metrics, options, NullLogger<TwoLevelCache>.Instance);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _memory.Dispose();
    }

    private static UserSkillSummaryDto Row(int id) =>
        new(id, Guid.NewGuid(), 1, "C#", "Offer", 3, "Active", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8]);

    [Fact]
    public async Task ListBySkill_SecondCallComesFromCache_WithRowVersionIntact()
    {
        var inner = Substitute.For<IUserSkillService>();
        inner.ListBySkillAsync(1, "Offer", 10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserSkillSummaryDto>>([Row(1)]));
        var service = new CachedUserSkillService(inner, _cache);

        await service.ListBySkillAsync(1, "Offer", 10);
        var second = await service.ListBySkillAsync(1, "Offer", 10);

        await inner.Received(1).ListBySkillAsync(1, "Offer", 10, Arg.Any<CancellationToken>());
        Assert.Equal(1, Assert.Single(second).Id);
        Assert.Equal(8, second[0].RowVersion.Length);
    }

    [Fact]
    public async Task Write_InvalidatesBySkillSelection()
    {
        var inner = Substitute.For<IUserSkillService>();
        inner.ListBySkillAsync(1, null, 10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserSkillSummaryDto>>([Row(1)]));
        var service = new CachedUserSkillService(inner, _cache);

        await service.ListBySkillAsync(1, null, 10);
        await service.DeleteAsync(Guid.NewGuid(), 1, [1]);
        await service.ListBySkillAsync(1, null, 10);

        await inner.Received(2).ListBySkillAsync(1, null, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserScopedReads_AreNotCached()
    {
        var inner = Substitute.For<IUserSkillService>();
        var service = new CachedUserSkillService(inner, _cache);
        var user = Guid.NewGuid();

        await service.GetByIdAsync(5);
        await service.GetByIdAsync(5);
        await service.ListAsync(user, null, null, 1, 20);
        await service.ListAsync(user, null, null, 1, 20);

        await inner.Received(2).GetByIdAsync(5, Arg.Any<CancellationToken>());
        await inner.Received(2).ListAsync(user, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReferenceData_IsLoadedOnce()
    {
        var inner = Substitute.For<IReferenceDataService>();
        inner.GetSkillsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SkillDto>>([new SkillDto(1, "C#")]));
        inner.GetLanguageAsync("uk", Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LanguageRefDto("uk", "Українська")));
        var service = new CachedReferenceDataService(inner, _cache);

        await service.GetSkillsAsync();
        var skills = await service.GetSkillsAsync();
        await service.GetLanguageAsync("uk");
        var language = await service.GetLanguageAsync("UK");

        await inner.Received(1).GetSkillsAsync(Arg.Any<CancellationToken>());
        Assert.Equal("C#", Assert.Single(skills).Name);
        Assert.Equal("uk", language.Code);
    }
}
