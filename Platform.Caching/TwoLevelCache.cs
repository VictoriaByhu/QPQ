using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Platform.Caching;

/// <summary>
/// Ручна реалізація схеми L1 (IMemoryCache) → L2 (Redis через IDistributedCache) → база даних.
/// DTO серіалізуються через System.Text.Json; ключі мають префікс сервісу (catalog:..., userskills:..., matching:...).
/// Збій Redis не валить запит: помилка логується, L2 на короткий час пропускається, дані читаються з бази.
/// (У .NET є готовий аналог — HybridCache; ця реалізація показує механізм, порівняння — у README.)
/// </summary>
public sealed class TwoLevelCache(
    IMemoryCache memory, IDistributedCache distributed, CacheMetrics metrics, CacheOptions options,
    ILogger<TwoLevelCache> logger)
    : ITwoLevelCache
{
    private static readonly TimeSpan L2Timeout = TimeSpan.FromMilliseconds(500);   // повільний Redis не гальмує запит
    private static readonly TimeSpan L2Cooldown = TimeSpan.FromSeconds(15);        // пауза L2 після збою
    private readonly string _versionPrefix = $"{options.KeyPrefix}:version:";

    private long _l2DisabledUntilTick;                                             // Environment.TickCount64
    private readonly Dictionary<string, string> _localVersions = new();            // якщо Redis недоступний

    public bool IsDistributedCacheAvailable => Environment.TickCount64 >= Interlocked.Read(ref _l2DisabledUntilTick);

    public async Task<T?> GetOrCreateAsync<T>(
        string key, CachePolicy policy, Func<CancellationToken, Task<T?>> factory, CancellationToken ct = default)
        where T : class
    {
        var sw = Stopwatch.StartNew();

        // ── L1 ──
        if (memory.TryGetValue(key, out T? local) && local is not null)
        {
            metrics.Hit(CacheMetrics.L1);
            metrics.Duration(CacheMetrics.L1, "get", sw.Elapsed.TotalMilliseconds);
            logger.LogDebug("Cache hit {Key} level {Level}", key, CacheMetrics.L1);
            return local;
        }
        metrics.Miss(CacheMetrics.L1);

        // ── L2 ──
        sw.Restart();
        var bytes = await TryAsync(() => distributed.GetAsync(key, ct), "get", key);
        if (bytes is not null)
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(bytes);
                if (value is not null)
                {
                    metrics.Hit(CacheMetrics.L2);
                    metrics.Duration(CacheMetrics.L2, "get", sw.Elapsed.TotalMilliseconds);
                    logger.LogDebug("Cache hit {Key} level {Level} size {Bytes} bytes", key, CacheMetrics.L2, bytes.Length);
                    SetLocal(key, value, policy);                              // L1 заповнюється на зворотному шляху
                    return value;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Cache entry {Key} is corrupted and will be rebuilt", key);
                await TryAsync(() => distributed.RemoveAsync(key, ct), "remove", key);
            }
        }
        if (IsDistributedCacheAvailable) metrics.Miss(CacheMetrics.L2);
        metrics.Duration(CacheMetrics.L2, "get", sw.Elapsed.TotalMilliseconds);
        logger.LogDebug("Cache miss {Key}", key);

        // ── база даних ──
        var created = await factory(ct);
        if (created is null) return null;

        var payload = JsonSerializer.SerializeToUtf8Bytes(created);
        await TryAsync(() => distributed.SetAsync(key, payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = policy.L2Absolute }, ct), "set", key);
        SetLocal(key, created, policy);
        logger.LogDebug("Cache set {Key} L1 ttl {L1Ttl} L2 ttl {L2Ttl} size {Bytes} bytes",
            key, policy.L1Absolute, policy.L2Absolute, payload.Length);
        return created;
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct = default)
    {
        memory.Remove(key);
        await TryAsync(() => distributed.RemoveAsync(key, ct), "remove", key);
        metrics.Invalidated(reason);
        logger.LogInformation("Cache invalidated {Key} reason {Reason}", key, reason);
    }

    public async Task<string> GetVersionAsync(string group, CancellationToken ct = default)
    {
        var remote = await TryAsync(() => distributed.GetStringAsync(_versionPrefix + group, ct), "get-version", group);
        if (!string.IsNullOrEmpty(remote)) return remote;
        lock (_localVersions) return _localVersions.GetValueOrDefault(group, "0");
    }

    public async Task BumpVersionAsync(string group, string reason, CancellationToken ct = default)
    {
        var version = DateTime.UtcNow.Ticks.ToString();                        // монотонно зростає між репліками
        lock (_localVersions) _localVersions[group] = version;
        await TryAsync(() => distributed.SetStringAsync(_versionPrefix + group, version,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) }, ct), "bump-version", group);
        metrics.Invalidated(reason);
        logger.LogInformation("Cache group {Group} moved to version {Version} reason {Reason}", group, version, reason);
    }

    private void SetLocal<T>(string key, T value, CachePolicy policy) where T : class =>
        memory.Set(key, value, CacheExtensions.CreateEntryOptions(policy, reason =>
        {
            if (reason is EvictionReason.Capacity or EvictionReason.Expired or EvictionReason.TokenExpired)
                metrics.Evicted(reason);
        }));

    /// <summary>Операція з L2 під захистом: таймаут, перехоплення помилок, пауза після збою.</summary>
    private async Task<TResult?> TryAsync<TResult>(Func<Task<TResult>> operation, string name, string key)
    {
        if (!IsDistributedCacheAvailable) return default;
        try
        {
            return await operation().WaitAsync(L2Timeout);
        }
        catch (OperationCanceledException)
        {
            throw;                                                              // скасував клієнт — це не збій Redis
        }
        catch (Exception ex)                                                    // у т.ч. TimeoutException від WaitAsync
        {
            Interlocked.Exchange(ref _l2DisabledUntilTick, Environment.TickCount64 + (long)L2Cooldown.TotalMilliseconds);
            logger.LogWarning(ex, "Distributed cache {Operation} failed for {Key}; L2 is skipped for {Cooldown} s",
                name, key, L2Cooldown.TotalSeconds);
            return default;
        }
    }

    private Task TryAsync(Func<Task> operation, string name, string key) =>
        TryAsync(async () => { await operation(); return true; }, name, key);
}
