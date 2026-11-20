using Messages.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Messages.WebApi.HealthChecks;

public static class MessagesHealthCheckExtensions
{
    /// <summary>
    /// Pings the service's MongoDB database with the client that the repositories already use,
    /// so the check sees the same connection pool and diagnostics as real requests.
    /// </summary>
    public static IServiceCollection AddMessagesHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddMongoDb(
                clientFactory: sp => sp.GetRequiredService<IMongoClient>(),
                databaseNameFactory: sp => sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.DatabaseName,
                name: "messages-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "mongodb", "ready"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
