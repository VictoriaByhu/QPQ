using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swaps.Api.HealthChecks;

public static class SwapsHealthCheckExtensions
{
    /// <summary>
    /// SQL Server is the source of truth, so its failure makes the service unready.
    /// Redis is only a cache level: when it is down the service still answers from the database, hence Degraded.
    /// </summary>
    public static IServiceCollection AddSwapsHealthChecks(
        this IServiceCollection services, string sqlConnectionString, string redisConnectionString)
    {
        // SqlClient does not abort a pending login on cancellation, so the check's own timeout is set on the connection.
        var probeConnectionString = new SqlConnectionStringBuilder(sqlConnectionString) { ConnectTimeout = 5 }.ConnectionString;

        services.AddHealthChecks()
            .AddSqlServer(probeConnectionString,
                healthQuery: "SELECT 1;",
                name: "swaps-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedis(redisConnectionString,
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        return services;
    }
}
