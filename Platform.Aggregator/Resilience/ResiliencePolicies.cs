using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Platform.Aggregator.Resilience;

/// <summary>Resilience settings of one downstream gRPC client.</summary>
public sealed record ClientPolicy(
    string Name,
    int HttpRetries,
    int GrpcMaxAttempts,
    TimeSpan GrpcMaxBackoff,
    TimeSpan AttemptTimeout,
    TimeSpan TotalTimeout,
    double FailureRatio,
    int MinimumThroughput,
    TimeSpan SamplingDuration,
    TimeSpan BreakDuration);

/// <summary>
/// Per-service policies. swaps-api is critical (no swap, no card), sessions-api and messages-api
/// only enrich the card, so they give up sooner. The values are explained in README (Lab 07).
/// </summary>
public static class ResiliencePolicies
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(0.5);

    public static readonly ClientPolicy Swaps = new(
        Name: "swaps-api",
        HttpRetries: 2,
        GrpcMaxAttempts: 5,
        GrpcMaxBackoff: TimeSpan.FromSeconds(4),
        AttemptTimeout: TimeSpan.FromSeconds(3),
        TotalTimeout: TimeSpan.FromSeconds(20),
        FailureRatio: 0.3,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(30),
        BreakDuration: TimeSpan.FromSeconds(15));

    public static readonly ClientPolicy Sessions = new(
        Name: "sessions-api",
        HttpRetries: 1,
        GrpcMaxAttempts: 3,
        GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalTimeout: TimeSpan.FromSeconds(8),
        FailureRatio: 0.5,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(20),
        BreakDuration: TimeSpan.FromSeconds(15));

    public static readonly ClientPolicy Messages = new(
        Name: "messages-api",
        HttpRetries: 1,
        GrpcMaxAttempts: 2,
        GrpcMaxBackoff: TimeSpan.FromSeconds(1),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalTimeout: TimeSpan.FromSeconds(5),
        FailureRatio: 0.7,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(10),
        BreakDuration: TimeSpan.FromSeconds(10));

    /// <summary>Replaces the default standard handler from ServiceDefaults with the client's own settings.</summary>
    public static IHttpClientBuilder WithResilience(this IHttpClientBuilder builder, ClientPolicy policy)
    {
        // Without the removal the handler below would stack on top of the default one.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is still marked experimental
        builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        builder.AddStandardResilienceHandler().Configure((options, sp) =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Platform.Aggregator.Resilience");

            options.Retry.MaxRetryAttempts = policy.HttpRetries;
            options.Retry.Delay = RetryDelay;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            AllowRetryOnlyForGrpcReads(options.Retry);

            options.AttemptTimeout.Timeout = policy.AttemptTimeout;
            options.TotalRequestTimeout.Timeout = policy.TotalTimeout;

            options.CircuitBreaker.FailureRatio = policy.FailureRatio;
            options.CircuitBreaker.MinimumThroughput = policy.MinimumThroughput;
            options.CircuitBreaker.SamplingDuration = policy.SamplingDuration;
            options.CircuitBreaker.BreakDuration = policy.BreakDuration;
            options.CircuitBreaker.OnOpened = args =>
            {
                logger.LogError(args.Outcome.Exception,
                    "Circuit breaker for {Client} opened for {BreakDuration}, last outcome {Outcome}",
                    policy.Name, args.BreakDuration,
                    args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name);
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnHalfOpened = _ =>
            {
                logger.LogInformation("Circuit breaker for {Client} is half-open after {BreakDuration}, probing",
                    policy.Name, policy.BreakDuration);
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnClosed = _ =>
            {
                logger.LogInformation("Circuit breaker for {Client} closed, calls flow normally", policy.Name);
                return ValueTask.CompletedTask;
            };
        });

        return builder;
    }

    /// <summary>
    /// Unsafe HTTP methods are never retried. gRPC sends every call as POST, so without an exception the HTTP
    /// retry would never fire for these clients; all our gRPC methods are reads, so gRPC requests keep the
    /// default transient-failure rules.
    /// </summary>
    private static void AllowRetryOnlyForGrpcReads(HttpRetryStrategyOptions retry)
    {
        var transient = retry.ShouldHandle;
        retry.DisableForUnsafeHttpMethods();
        var safeMethodsOnly = retry.ShouldHandle;

        retry.ShouldHandle = args => IsGrpc(args.Context.GetRequestMessage()) ? transient(args) : safeMethodsOnly(args);
    }

    private static bool IsGrpc(HttpRequestMessage? request)
        => request?.Content?.Headers.ContentType?.MediaType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// gRPC statuses travel in trailers of an HTTP 200, so the HTTP handler cannot see them;
    /// the channel retries them itself. All our methods are reads, so a retry is always safe.
    /// </summary>
    public static IHttpClientBuilder WithGrpcRetry(this IHttpClientBuilder builder, ClientPolicy policy)
        => builder.ConfigureChannel(channel => channel.ServiceConfig = new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = policy.GrpcMaxAttempts,
                        InitialBackoff = RetryDelay,
                        MaxBackoff = policy.GrpcMaxBackoff,
                        BackoffMultiplier = 2,
                        RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                    }
                }
            }
        });

    /// <summary>The call deadline covers every retry of the client, so it equals the total timeout.</summary>
    public static DateTime Deadline(this ClientPolicy policy) => DateTime.UtcNow.Add(policy.TotalTimeout);
}
