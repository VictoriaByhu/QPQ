using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Platform.Aggregator.Services;

/// <summary>
/// Розподілений кеш зібраних відповідей (Redis). TTL короткий і навмисно менший, ніж у кешах окремих сервісів:
/// інакше агрегована відповідь виглядала б «свіжішою», ніж дані, з яких її зібрано.
/// Збій Redis не валить запит — Aggregator просто збирає відповідь заново (з паузою L2 після помилки).
/// Метрики: Meter "Aggregator.Cache" (cache.hits / cache.misses).
/// </summary>
public sealed class AggregateCache(IDistributedCache cache, ILogger<AggregateCache> logger)
{
    private static readonly Meter Meter = new("Aggregator.Cache");
    private static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits");
    private static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses");
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(15);

    private long _disabledUntilTick;

    public static TimeSpan Ttl { get; } = TimeSpan.FromSeconds(30);
    public static string SkillKey(int skillId) => $"aggregator:skill:{skillId}:full";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (!Available) return null;
        try
        {
            var bytes = await cache.GetAsync(key, ct).WaitAsync(Timeout, ct);
            if (bytes is null)
            {
                Misses.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
                logger.LogDebug("Aggregate cache miss {Key}", key);
                return null;
            }

            Hits.Add(1, new KeyValuePair<string, object?>("cache.level", "L2"));
            logger.LogDebug("Aggregate cache hit {Key} size {Bytes} bytes", key, bytes.Length);
            return JsonSerializer.Deserialize<T>(bytes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || ex is TimeoutException)
        {
            Suspend(ex, "get", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken ct) where T : class
    {
        if (!Available) return;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await cache.SetAsync(key, bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }, ct).WaitAsync(Timeout, ct);
            logger.LogDebug("Aggregate cache set {Key} ttl {Ttl} s size {Bytes} bytes", key, Ttl.TotalSeconds, bytes.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || ex is TimeoutException)
        {
            Suspend(ex, "set", key);
        }
    }

    private bool Available => Environment.TickCount64 >= Interlocked.Read(ref _disabledUntilTick);

    private void Suspend(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _disabledUntilTick, Environment.TickCount64 + (long)Cooldown.TotalMilliseconds);
        logger.LogWarning(ex, "Redis {Operation} failed for {Key}; aggregate cache is skipped for {Cooldown} s",
            operation, key, Cooldown.TotalSeconds);
    }
}
