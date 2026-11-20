using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public static class AggregatorHealthCheckExtensions
{
    private static readonly string[] DownstreamTags = ["grpc", "downstream", "ready"];

    public static IServiceCollection AddAggregatorHealthChecks(this IServiceCollection services, string redisConnectionString)
    {
        // Redis only caches complete aggregates; without it every request is assembled from the services, hence Degraded.
        var checks = services.AddHealthChecks()
            .AddRedis(redisConnectionString,
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        // The failure status follows how critical the service is for the card (see README, Lab 07).
        services.AddDownstreamCheck(checks, "swaps-api", HealthStatus.Unhealthy);
        services.AddDownstreamCheck(checks, "sessions-api", HealthStatus.Degraded);
        services.AddDownstreamCheck(checks, "messages-api", HealthStatus.Degraded);

        return services;
    }

    private static void AddDownstreamCheck(
        this IServiceCollection services, IHealthChecksBuilder checks, string serviceName, HealthStatus failureStatus)
    {
        var clientName = $"{serviceName}-health";

        // A probe reports the current state, so it is neither retried nor held back by a circuit breaker.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is still marked experimental
        services.AddGrpcClient<Health.HealthClient>(clientName, o => o.Address = new Uri($"https://{serviceName}"))
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        checks.Add(new HealthCheckRegistration(
            serviceName,
            sp => new DownstreamGrpcHealthCheck(
                sp.GetRequiredService<GrpcClientFactory>().CreateClient<Health.HealthClient>(clientName),
                serviceName,
                sp.GetRequiredService<ILogger<DownstreamGrpcHealthCheck>>()),
            failureStatus,
            DownstreamTags));
    }
}
