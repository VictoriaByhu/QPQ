using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Включаємо Service Discovery Aspire для YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();

// === ДОДАНА ЧАСТИНА: Middleware для CorrelationId та Логування маршрутизації ===
app.Use(async (context, next) =>
{
    // 1. Беремо існуючий Correlation-Id або генеруємо новий
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

    // 2. Прокидаємо в поточний запит (щоб YARP передав його далі сервісам)
    context.Request.Headers.TryAdd("X-Correlation-Id", correlationId);

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sw = System.Diagnostics.Stopwatch.StartNew();

    // Передаємо управління далі по pipeline (до YARP)
    await next();

    sw.Stop();

    // 3. Зчитуємо інформацію про маршрут YARP (якщо запит був смаршрутизований)
    var endpoint = context.GetEndpoint();
    var routeName = endpoint?.DisplayName ?? "No Route Match";

    // 4. Логуємо необхідні за критеріями дані
    logger.LogInformation(
        "Gateway Routing -> Method: {Method}, Path: {Path}, Route: {Route}, Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
        context.Request.Method,
        context.Request.Path,
        routeName,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        correlationId);
});
// ==============================================================================

app.MapReverseProxy();

app.Run();