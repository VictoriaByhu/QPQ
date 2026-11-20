using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sessions.Api.HealthChecks;

public static class SessionsHealthCheckExtensions
{
    /// <summary>Without SQL Server the service cannot answer anything, so its failure makes it unready.</summary>
    public static IServiceCollection AddSessionsHealthChecks(this IServiceCollection services, string sqlConnectionString)
    {
        // SqlClient does not abort a pending login on cancellation, so the check's own timeout is set on the connection.
        var probeConnectionString = new SqlConnectionStringBuilder(sqlConnectionString) { ConnectTimeout = 5 }.ConnectionString;

        services.AddHealthChecks()
            .AddSqlServer(probeConnectionString,
                healthQuery: "SELECT 1;",
                name: "sessions-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
