namespace Platform.Caching;

/// <summary>Налаштування кешу сервісу: ім'я визначає префікс ключів (catalog:...) і назву Meter (Catalog.Cache).</summary>
public sealed record CacheOptions(string Service)
{
    public string KeyPrefix => Service.ToLowerInvariant();
    public string MeterName => $"{Service}.Cache";
}
