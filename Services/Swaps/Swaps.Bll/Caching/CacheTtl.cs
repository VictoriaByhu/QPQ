using Microsoft.Extensions.Caching.Memory;

namespace Swaps.Bll.Caching;

/// <summary>Lifetimes of a cache entry. L1 must stay shorter than L2.</summary>
public readonly record struct CacheTtl(TimeSpan L1, TimeSpan L2, CacheItemPriority Priority = CacheItemPriority.Normal)
{
    public static readonly CacheTtl Skills = new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), CacheItemPriority.High);

    public static readonly CacheTtl SwapCard = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
}
