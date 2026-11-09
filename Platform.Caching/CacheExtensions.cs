using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Platform.Caching;

public static class CacheExtensions
{
    /// <summary>
    /// Підключає двохрівневий кеш сервісу. L1 — IMemoryCache з обмеженням розміру (кожен запис має Size, найстаріші
    /// витісняються за пріоритетом і терміном життя); L2 — Redis через Aspire (ресурс "redis" в AppHost ->
    /// ConnectionStrings:redis). Без рядка підключення (запуск поза Aspire, тести) L2 замінює IDistributedCache у пам'яті.
    /// </summary>
    public static IHostApplicationBuilder AddTwoLevelCache(
        this IHostApplicationBuilder builder, string service, int sizeLimit = 10_000)
    {
        builder.Services.AddMemoryCache(o =>
        {
            o.SizeLimit = sizeLimit;                // у «одиницях» Size записів
            o.TrackStatistics = true;               // для метрик розміру L1 і health check
        });
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
            builder.AddRedisDistributedCache("redis");
        else
            builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSingleton(new CacheOptions(service));
        builder.Services.AddSingleton<CacheMetrics>();
        builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();
        builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");   // статистика кешу
        return builder;
    }

    /// <summary>GET /health/cache — JSON зі статистикою кешу (hits/misses/hitRatio за рівнями, доступність Redis).</summary>
    public static WebApplication MapCacheHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/cache", new HealthCheckOptions
        {
            Predicate = r => r.Name == "cache",
            ResponseWriter = CacheHealthCheck.WriteResponse
        });
        return app;
    }
}
