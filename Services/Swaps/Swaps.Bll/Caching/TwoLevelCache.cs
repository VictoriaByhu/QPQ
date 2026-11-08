using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Swaps.Bll.Caching;

/// <summary>Cache-aside over process memory (L1) and Redis (L2). Redis failures degrade to the database.</summary>
public sealed class TwoLevelCache(
    IMemoryCache memory,
    IDistributedCache distributed,
    CacheMetrics metrics,
    ILogger<TwoLevelCache> logger)
{
    private const string L1 = "L1";
    private const string L2 = "L2";
    private const int EntrySize = 1;

    // Each failed Redis call can block for the client timeout, so reads skip Redis for a while after a failure.
    private static readonly TimeSpan RedisCooldown = TimeSpan.FromSeconds(30);

    private long _redisRetryAtTicks;

    public async Task<T?> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T?>> factory, CacheTtl ttl, CancellationToken ct)
    {
        if (memory.TryGetValue(key, out T? value))
        {
            metrics.Hit(L1);
            logger.LogInformation("Cache hit {Key} at {Level}", key, L1);
            return value;
        }

        metrics.Miss(L1);

        var bytes = await TryGetRedisAsync(key, ct);
        if (bytes is not null && TryDeserialize(key, bytes, out value))
        {
            metrics.Hit(L2);
            logger.LogInformation("Cache hit {Key} at {Level}", key, L2);
            SetL1(key, value, ttl);
            return value;
        }

        metrics.Miss(L2);
        logger.LogInformation("Cache miss {Key}", key);

        value = await factory(ct);
        if (value is null)
            return default;

        await TrySetRedisAsync(key, JsonSerializer.SerializeToUtf8Bytes(value), ttl, ct);
        SetL1(key, value, ttl);
        return value;
    }

    /// <summary>
    /// Removes the keys from both levels. Runs right after a committed write, so callers pass
    /// <see cref="CancellationToken.None"/> and a cancelled request cannot leave stale entries behind.
    /// </summary>
    public async Task InvalidateAsync(string reason, CancellationToken ct, params string[] keys)
    {
        foreach (var key in keys)
        {
            memory.Remove(key);
            await TryRemoveRedisAsync(key, ct);
            metrics.Invalidated();
            logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
        }
    }

    private void SetL1<T>(string key, T? value, CacheTtl ttl)
    {
        memory.Set(key, value, new MemoryCacheEntryOptions
        {
            Size = EntrySize,
            AbsoluteExpirationRelativeToNow = ttl.L1,
            SlidingExpiration = ttl.L1 / 2,
            Priority = ttl.Priority
        });
        logger.LogInformation("Cache set {Key} ttl {Ttl} size {Size}", key, ttl.L1, EntrySize);
    }

    private bool TryDeserialize<T>(string key, byte[] bytes, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(bytes);
            return value is not null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cache entry {Key} has an unexpected format", key);
            value = default;
            return false;
        }
    }

    private async Task<byte[]?> TryGetRedisAsync(string key, CancellationToken ct)
    {
        if (IsRedisCoolingDown())
            return null;

        try
        {
            return await distributed.GetAsync(key, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnRedisFailure(ex, "read", key);
            return null;
        }
    }

    private async Task TrySetRedisAsync(string key, byte[] bytes, CacheTtl ttl, CancellationToken ct)
    {
        if (IsRedisCoolingDown())
            return;

        try
        {
            await distributed.SetAsync(key, bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl.L2 }, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnRedisFailure(ex, "write", key);
        }
    }

    // Unlike reads, removal ignores the cooldown: a skipped removal would serve stale data once Redis is back.
    private async Task TryRemoveRedisAsync(string key, CancellationToken ct)
    {
        try
        {
            await distributed.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnRedisFailure(ex, "remove", key);
        }
    }

    private bool IsRedisCoolingDown() => DateTime.UtcNow.Ticks < Interlocked.Read(ref _redisRetryAtTicks);

    private void OnRedisFailure(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _redisRetryAtTicks, DateTime.UtcNow.Add(RedisCooldown).Ticks);
        logger.LogWarning(ex, "Redis {Operation} failed for {Key}, continuing without L2", operation, key);
    }
}
