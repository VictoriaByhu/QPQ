using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Caching;

/// <summary>Redis cache of complete aggregated responses. Redis failures only cost a cache miss.</summary>
public sealed class FullSwapCache(
    IDistributedCache cache,
    AggregatorCacheMetrics metrics,
    ILogger<FullSwapCache> logger)
{
    // Shorter than the service caches, so the aggregate never outlives its sources for long.
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    // Each failed Redis call can block for the client timeout, so Redis is skipped for a while after a failure.
    private static readonly TimeSpan RedisCooldown = TimeSpan.FromSeconds(30);

    private long _redisRetryAtTicks;

    public async Task<FullSwapDto?> TryGetAsync(int swapId, Guid userId, CancellationToken ct)
    {
        var key = Key(swapId, userId);
        if (!IsRedisCoolingDown())
        {
            try
            {
                var bytes = await cache.GetAsync(key, ct);
                if (bytes is not null && JsonSerializer.Deserialize<FullSwapDto>(bytes, JsonSerializerOptions.Web) is { } cached)
                {
                    metrics.Hit();
                    logger.LogInformation("Cache hit {Key} at {Level}", key, "L2");
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Cache entry {Key} has an unexpected format", key);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                OnRedisFailure(ex, "read", key);
            }
        }

        metrics.Miss();
        logger.LogInformation("Cache miss {Key}", key);
        return null;
    }

    public async Task SetAsync(int swapId, Guid userId, FullSwapDto value, CancellationToken ct)
    {
        if (IsRedisCoolingDown())
            return;

        var key = Key(swapId, userId);
        try
        {
            await cache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
                ct);
            logger.LogInformation("Cache set {Key} ttl {Ttl}", key, Ttl);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnRedisFailure(ex, "write", key);
        }
    }

    private static string Key(int swapId, Guid userId) => $"aggregator:swap:{swapId}:full:{userId:D}";

    private bool IsRedisCoolingDown() => DateTime.UtcNow.Ticks < Interlocked.Read(ref _redisRetryAtTicks);

    private void OnRedisFailure(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _redisRetryAtTicks, DateTime.UtcNow.Add(RedisCooldown).Ticks);
        logger.LogWarning(ex, "Redis {Operation} failed for {Key}, continuing without cache", operation, key);
    }
}
