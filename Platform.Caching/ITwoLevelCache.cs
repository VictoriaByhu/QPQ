namespace Platform.Caching;

/// <summary>
/// Час життя запису на двох рівнях. L1 (пам'ять) має жити коротше за L2 (Redis): після оновлення в Redis
/// застаріла копія в пам'яті інакше пережила б його. Конструктор перевіряє це правило.
/// </summary>
public sealed record CachePolicy
{
    public TimeSpan L1Absolute { get; }
    public TimeSpan? L1Sliding { get; }
    public TimeSpan L2Absolute { get; }

    public CachePolicy(TimeSpan l1Absolute, TimeSpan l2Absolute, TimeSpan? l1Sliding = null)
    {
        if (l1Absolute >= l2Absolute)
            throw new ArgumentException("L1 TTL must be shorter than L2 TTL, otherwise stale in-memory data outlives Redis.");
        L1Absolute = l1Absolute;
        L2Absolute = l2Absolute;
        L1Sliding = l1Sliding;
    }

    /// <summary>Часто читані дані, що змінюються епізодично (картка навички): sliding продовжує життя гарячих записів.</summary>
    public static CachePolicy Entity { get; } = new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(45));

    /// <summary>Сторінки списків: ключ містить версію, тож стара версія просто перестає читатися; TTL короткий.</summary>
    public static CachePolicy List { get; } = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

    /// <summary>Довідники (теги, категорії): рідко змінюються — absolute expiration без sliding.</summary>
    public static CachePolicy Reference { get; } = new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(30));
}

/// <summary>Двохрівневий кеш: L1 = IMemoryCache, L2 = IDistributedCache (Redis, спільний для всіх реплік).</summary>
public interface ITwoLevelCache
{
    /// <summary>
    /// Cache-aside: L1 → L2 → factory (база даних); кожен рівень заповнюється на зворотному шляху.
    /// Недоступність L2 не ламає виклик — читання просто йде до наступного рівня. null не кешується.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string key, CachePolicy policy, Func<CancellationToken, Task<T?>> factory, CancellationToken ct = default)
        where T : class;

    /// <summary>Синхронно видаляє запис з обох рівнів (викликається одразу після запису в базу).</summary>
    Task RemoveAsync(string key, string reason, CancellationToken ct = default);

    /// <summary>Поточна версія групи ключів (наприклад, усіх списків навичок), що входить у ключ.</summary>
    Task<string> GetVersionAsync(string group, CancellationToken ct = default);

    /// <summary>Нова версія групи: усі ключі зі старою версією стають недосяжними (масова інвалідація).</summary>
    Task BumpVersionAsync(string group, string reason, CancellationToken ct = default);

    /// <summary>Чи вважається L2 доступним (після збою Redis він тимчасово пропускається).</summary>
    bool IsDistributedCacheAvailable { get; }
}
