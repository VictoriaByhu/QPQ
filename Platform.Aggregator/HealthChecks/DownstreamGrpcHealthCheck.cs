using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

/// <summary>
/// Asks a downstream service over grpc.health.v1 whether it is serving. A failure reports the status the
/// registration chose for that service: Unhealthy for swaps-api, Degraded for the services that only enrich the card.
/// </summary>
public sealed class DownstreamGrpcHealthCheck(
    Health.HealthClient client,
    string serviceName,
    ILogger<DownstreamGrpcHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var failureStatus = context.Registration.FailureStatus;
        try
        {
            var reply = await client.CheckAsync(
                new HealthCheckRequest(), deadline: DateTime.UtcNow.Add(Deadline), cancellationToken: ct);

            if (reply.Status == HealthCheckResponse.Types.ServingStatus.Serving)
                return HealthCheckResult.Healthy($"{serviceName} is serving");

            logger.LogWarning("gRPC health check of {Service} returned {ServingStatus}", serviceName, reply.Status);
            return new HealthCheckResult(failureStatus, $"{serviceName} reports {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning("gRPC health check of {Service} failed with {StatusCode}: {Detail}",
                serviceName, ex.StatusCode, ex.Status.Detail);
            return new HealthCheckResult(failureStatus, $"{serviceName} is unreachable ({ex.StatusCode})", ex);
        }
    }
}
