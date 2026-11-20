using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using QPQ.Reviews.Application;
using QPQ.Reviews.Application.Caching;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Infrastructure;
using QPQ.Reviews.WebApi.Grpc;
using QPQ.Reviews.WebApi.Metrics;

var builder = WebApplication.CreateBuilder(args);

// 1. Інтеграція .NET Aspire (ServiceDefaults)
builder.AddServiceDefaults();

// 2. Підключення баз даних та кешу
builder.AddMongoDBClient("ReviewsDb");
builder.AddRedisDistributedCache("redis");

// Реєстрація мапінгу MongoDB
QPQ.Reviews.Infrastructure.Persistence.MongoMappings.Register();

// Реєстрація шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<IReviewQueries, QPQ.Reviews.Infrastructure.Repositories.ReviewQueries>();

builder.Services.AddControllers();

// ==========================================
// 3. Кешування, gRPC та Метрики (Лаб 06–07)
// ==========================================
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<IReviewsMetrics, ReviewsMetrics>();

builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);

builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

// ==========================================
// 4. Health Checks (Лаб 07: MongoDB, Redis, gRPC)
// ==========================================
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "reviews-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "mongodb", "ready"],
        timeout: TimeSpan.FromSeconds(5))
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("redis")!,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded, // кеш необов'язковий
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3));

builder.Services.AddGrpcHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<QPQ.Reviews.Infrastructure.Seeding.IDataSeeder, QPQ.Reviews.Infrastructure.Seeding.ReviewSeeder>();
builder.Services.AddHostedService<QPQ.Reviews.Infrastructure.Seeding.IndexCreationService>();

var app = builder.Build();

app.UseMiddleware<QPQ.Reviews.WebApi.Middleware.ExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<QPQ.Reviews.Infrastructure.Seeding.IDataSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 5. Мапінг ендпоінтів (Health Checks + gRPC)
app.MapDefaultEndpoints();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();

app.Run();