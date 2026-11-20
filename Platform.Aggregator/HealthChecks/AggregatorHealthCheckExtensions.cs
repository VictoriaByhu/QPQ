using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public static class AggregatorHealthCheckExtensions
{
    /// <summary>Redis only caches complete aggregates; without it every request is assembled from the services, hence Degraded.</summary>
    public static IServiceCollection AddAggregatorHealthChecks(this IServiceCollection services, string redisConnectionString)
    {
        services.AddHealthChecks()
            .AddRedis(redisConnectionString,
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        return services;
    }
}
