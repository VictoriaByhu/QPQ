using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Підключаємо нашу інфраструктуру (Serilog, OpenTelemetry, HealthChecks)
builder.AddServiceDefaults();

// Залишаємо OpenAPI з твого початкового шаблону
builder.Services.AddOpenApi();

// 2. Налаштування Композитного DTO (camelCase та ігнорування null-значень)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 3. Реєстрація Typed HttpClients через Service Discovery Aspire
builder.Services.AddHttpClient<ReviewsClient>(client =>
    client.BaseAddress = new Uri("http://reviews-api"));

// Заглушка для SwapService, поки він вимкнений у Solution
builder.Services.AddHttpClient<SwapClient>(client =>
    client.BaseAddress = new Uri("http://swap-service"));

var app = builder.Build();

// Маршрути для HealthChecks та Aspire
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 4. Агрегувальний ендпоінт із паралельними викликами Task.WhenAll
app.MapGet("/api/aggregator/composite/{id}", async (
    string id,
    ReviewsClient reviewsClient,
    SwapClient swapClient,
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
{
    var result = new CompositeDto();
    var sw = System.Diagnostics.Stopwatch.StartNew();

    // Запускаємо виклики паралельно (БЕЗ використання .Result або Task.WaitAll)
    var reviewsTask = reviewsClient.GetReviewsAsync(id, cancellationToken);
    var swapTask = swapClient.GetSwapDataAsync(id, cancellationToken);

    try
    {
        // Чекаємо завершення всіх запитів одночасно
        await Task.WhenAll(reviewsTask, swapTask);
    }
    catch (Exception ex)
    {
        // 5. Часткові відмови: якщо один сервіс впав, ми не валимо весь Агрегатор (немає помилки 500)
        logger.LogWarning(ex, "Один із сервісів відхилив запит або недоступний.");
    }

    sw.Stop();

    // Збираємо дані від Reviews (перевіряємо, чи таска успішна)
    if (reviewsTask.IsCompletedSuccessfully && reviewsTask.Result != null)
    {
        result.Reviews = reviewsTask.Result;
        logger.LogInformation("Reviews fetched in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
    else
    {
        result.Warnings.Add("Сервіс відгуків тимчасово недоступний.");
    }

    // Збираємо дані від Swaps
    if (swapTask.IsCompletedSuccessfully && swapTask.Result != null)
    {
        result.Swaps = swapTask.Result;
        logger.LogInformation("Swaps fetched in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
    else
    {
        result.Warnings.Add("Сервіс обміну (SwapService) тимчасово недоступний.");
    }

    return Results.Ok(result);
});

app.Run();

// ==========================================
// ДОПОМІЖНІ КЛАСИ ДЛЯ АГРЕГАТОРА (Typed HttpClients та DTO)
// ==========================================

public class CompositeDto
{
    public object? Reviews { get; set; }
    public object? Swaps { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class ReviewsClient(HttpClient httpClient, ILogger<ReviewsClient> logger)
{
    public async Task<object?> GetReviewsAsync(string id, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync($"/api/reviews/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Call to Reviews API took {Elapsed}ms", sw.ElapsedMilliseconds);
            return await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Помилка при виклику Reviews API");
            throw;
        }
    }
}

public class SwapClient(HttpClient httpClient, ILogger<SwapClient> logger)
{
    public async Task<object?> GetSwapDataAsync(string id, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await httpClient.GetAsync($"/api/swaps/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Call to Swaps API took {Elapsed}ms", sw.ElapsedMilliseconds);
            return await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Помилка при виклику Swaps API");
            throw;
        }
    }
}