using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using QPQ.Reviews.Grpc; // Неймспейс з нашого згенерованого .proto файлу

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

// ==========================================
// ЛАБ 06: РЕЄСТРАЦІЯ gRPC ТА REDIS
// ==========================================
// Підключаємо Redis для кешування результатів агрегатора
builder.AddRedisDistributedCache("redis");

// Реєструємо типізований gRPC-клієнт через Service Discovery Aspire
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(client =>
{
    client.Address = new Uri("https://reviews-api");
});

// Заглушка для SwapService (залишаємо на HTTP, бо він поки вимкнений)
builder.Services.AddHttpClient<SwapClient>(client =>
    client.BaseAddress = new Uri("http://swap-service"));

var app = builder.Build();

app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

// ==========================================
// 4. Агрегувальний ендпоінт (gRPC + Redis Кеш)
// ==========================================
app.MapGet("/api/aggregator/composite/{id}", async (
    string id,
    ReviewsService.ReviewsServiceClient reviewsGrpcClient,
    SwapClient swapClient,
    IDistributedCache cache,
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
{
    // ЛАБ 06: Перевіряємо кеш Агрегатора ПЕРЕД викликами
    var cacheKey = $"aggregator:composite:{id}";
    var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

    if (!string.IsNullOrEmpty(cachedData))
    {
        logger.LogInformation("Aggregator cache HIT for {CacheKey}", cacheKey);
        var cachedResult = JsonSerializer.Deserialize<CompositeDto>(cachedData);
        return Results.Ok(cachedResult);
    }

    logger.LogInformation("Aggregator cache MISS for {CacheKey}. Fetching data...", cacheKey);

    var result = new CompositeDto();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var warnings = new List<string>();

    // Готуємо gRPC-запит для Відгуків
    var reviewRequest = new GetReviewRequest { Id = id };

    // Запускаємо виклики паралельно
    var reviewsTask = reviewsGrpcClient.GetReviewAsync(reviewRequest, cancellationToken: cancellationToken).ResponseAsync;
    var swapTask = swapClient.GetSwapDataAsync(id, cancellationToken);

    try
    {
        // Чекаємо завершення всіх запитів паралельно
        await Task.WhenAll(reviewsTask, swapTask);
    }
    catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
    {
        logger.LogWarning(ex, "gRPC сервіс відгуків недоступний.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Один із сервісів (HTTP) відхилив запит або недоступний.");
    }

    sw.Stop();

    // Збираємо дані від Reviews (gRPC)
    if (reviewsTask.IsCompletedSuccessfully && reviewsTask.Result != null)
    {
        result.Reviews = reviewsTask.Result;
        logger.LogInformation("Reviews fetched via gRPC in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
    else
    {
        result.Warnings.Add("Сервіс відгуків тимчасово недоступний.");
    }

    // Збираємо дані від Swaps (HTTP)
    if (swapTask.IsCompletedSuccessfully && swapTask.Result != null)
    {
        result.Swaps = swapTask.Result;
    }
    else
    {
        result.Warnings.Add("Сервіс обміну (SwapService) тимчасово недоступний.");
    }

    // ЛАБ 06: Кешуємо агреговану відповідь у Redis (тільки якщо немає попереджень)
    if (result.Warnings.Count == 0)
    {
        var serializedResult = JsonSerializer.Serialize(result);
        var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }; // Короткий TTL
        await cache.SetStringAsync(cacheKey, serializedResult, cacheOptions, cancellationToken);
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
            throw; // Task.WhenAll перехопить це як помилку таски
        }
    }
}