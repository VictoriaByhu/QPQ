#pragma warning disable EXTEXP0001
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using QPQ.Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

// 1. Інфраструктура
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// 2. Налаштування Композитного DTO
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 3. Redis для кешування результатів агрегатора
builder.AddRedisDistributedCache("redis");

// ==========================================
// ЛАБ 07: RESILIENCE ДЛЯ КЛІЄНТІВ AGGREGATOR
// ==========================================

// Некритичний сервіс: Reviews (gRPC) — швидко здатися й повернути часткову відповідь
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(client =>
{
    client.Address = new Uri("https://reviews-api");
})
    .ConfigureChannel(channel =>
    {
        // Retry-політика на рівні gRPC-статусів (HTTP-обробник не бачить статуси в трейлерах)
        channel.ServiceConfig = new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = 3,
                        InitialBackoff = TimeSpan.FromSeconds(0.5),
                        MaxBackoff = TimeSpan.FromSeconds(2),
                        BackoffMultiplier = 2,
                        RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                    }
                }
            }
        };
    })
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 1;
        o.Retry.Delay = TimeSpan.FromSeconds(0.5);
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.Retry.UseJitter = true;
        o.Retry.DisableForUnsafeHttpMethods();

        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);

        o.CircuitBreaker.FailureRatio = 0.7; // менш чутливий (некритичний сервіс)
        o.CircuitBreaker.MinimumThroughput = 5;
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10); // >= 2 * AttemptTimeout
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        o.CircuitBreaker.OnOpened = args =>
        {
            Serilog.Log.Error("Circuit breaker OPENED for reviews-api for {Duration}", args.BreakDuration);
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnHalfOpened = _ =>
        {
            Serilog.Log.Warning("Circuit breaker HALF-OPENED for reviews-api");
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnClosed = _ =>
        {
            Serilog.Log.Information("Circuit breaker CLOSED for reviews-api");
            return ValueTask.CompletedTask;
        };
    });

// Критичний сервіс: SwapService (HTTP) — більше повторів і чутливіший Circuit Breaker
builder.Services.AddHttpClient<SwapClient>(client =>
{
    client.BaseAddress = new Uri("http://swap-service");
})
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 4;
        o.Retry.Delay = TimeSpan.FromSeconds(1);
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.Retry.UseJitter = true;
        o.Retry.DisableForUnsafeHttpMethods();

        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);

        o.CircuitBreaker.FailureRatio = 0.3; // чутливіший (критичний сервіс)
        o.CircuitBreaker.MinimumThroughput = 5;
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30); // >= 2 * AttemptTimeout
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        o.CircuitBreaker.OnOpened = args =>
        {
            Serilog.Log.Error("Circuit breaker OPENED for swap-service for {Duration}", args.BreakDuration);
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnHalfOpened = _ =>
        {
            Serilog.Log.Warning("Circuit breaker HALF-OPENED for swap-service");
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnClosed = _ =>
        {
            Serilog.Log.Information("Circuit breaker CLOSED for swap-service");
            return ValueTask.CompletedTask;
        };
    });

// ==========================================
// ЛАБ 07: HEALTH CHECKS (REDIS + gRPC)
// ==========================================
builder.Services.AddGrpcClient<Health.HealthClient>(o =>
{
    o.Address = new Uri("https://reviews-api");
});

builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("redis")!,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded, // кеш необов'язковий
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck<ReviewsGrpcHealthCheck>(
        name: "reviews-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["grpc", "downstream", "ready"]);

var app = builder.Build();

app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

// ==========================================
// 4. Агрегувальний ендпоінт (gRPC + Redis Graceful Degradation)
// ==========================================
app.MapGet("/api/aggregator/composite/{id}", async (
    string id,
    ReviewsService.ReviewsServiceClient reviewsGrpcClient,
    SwapClient swapClient,
    IDistributedCache cache,
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
{
    var cacheKey = $"aggregator:composite:{id}";
    string? cachedData = null;

    // ЛАБ 07: Graceful degradation при читанні з Redis
    try
    {
        cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Redis unavailable on GET, falling back to downstream services for {CacheKey}", cacheKey);
    }

    if (!string.IsNullOrEmpty(cachedData))
    {
        logger.LogInformation("Aggregator cache HIT for {CacheKey}", cacheKey);
        var cachedResult = JsonSerializer.Deserialize<CompositeDto>(cachedData);
        return Results.Ok(cachedResult);
    }

    logger.LogInformation("Aggregator cache MISS for {CacheKey}. Fetching data...", cacheKey);

    var result = new CompositeDto();
    var sw = System.Diagnostics.Stopwatch.StartNew();

    var reviewRequest = new GetReviewRequest { Id = id };

    var reviewsTask = reviewsGrpcClient.GetReviewAsync(reviewRequest, cancellationToken: cancellationToken).ResponseAsync;
    var swapTask = swapClient.GetSwapDataAsync(id, cancellationToken);

    try
    {
        await Task.WhenAll(reviewsTask, swapTask);
    }
    catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
    {
        logger.LogWarning(ex, "gRPC сервіс відгуків недоступний.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Один із сервісів відхилив запит або недоступний.");
    }

    sw.Stop();

    if (reviewsTask.IsCompletedSuccessfully && reviewsTask.Result != null)
    {
        result.Reviews = reviewsTask.Result;
        logger.LogInformation("Reviews fetched via gRPC in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
    else
    {
        result.Warnings.Add("Сервіс відгуків тимчасово недоступний.");
    }

    if (swapTask.IsCompletedSuccessfully && swapTask.Result != null)
    {
        result.Swaps = swapTask.Result;
    }
    else
    {
        result.Warnings.Add("Сервіс обміну (SwapService) тимчасово недоступний.");
    }

    // ЛАБ 07: Graceful degradation при записі в Redis
    if (result.Warnings.Count == 0)
    {
        try
        {
            var serializedResult = JsonSerializer.Serialize(result);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };
            await cache.SetStringAsync(cacheKey, serializedResult, cacheOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable on SET, value for {CacheKey} not cached", cacheKey);
        }
    }

    return Results.Ok(result);
});

app.Run();

// ==========================================
// ДОПОМІЖНІ КЛАСИ
// ==========================================

public class CompositeDto
{
    public object? Reviews { get; set; }
    public object? Swaps { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class SwapClient(HttpClient httpClient, ILogger<SwapClient> logger)
{
    public async Task<object?> GetSwapDataAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/swaps/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Помилка при виклику Swaps API");
            throw;
        }
    }
}

public class ReviewsGrpcHealthCheck(Health.HealthClient client, ILogger<ReviewsGrpcHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.AddSeconds(2),
                cancellationToken: cancellationToken);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("reviews-api gRPC is serving")
                : HealthCheckResult.Unhealthy($"reviews-api gRPC status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "reviews-api gRPC health check failed");
            return HealthCheckResult.Unhealthy("reviews-api gRPC is unavailable", ex);
        }
    }
}