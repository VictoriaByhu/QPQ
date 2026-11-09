using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QPQ.Reviews.Application;
using QPQ.Reviews.Infrastructure;
using QPQ.Reviews.WebApi.Grpc; // Простір імен для gRPC сервісу та Інтерцептора
using QPQ.Reviews.Application.Caching; // Простір імен для Метрик кешу

var builder = WebApplication.CreateBuilder(args);

// 1. Інтеграція .NET Aspire (Лаб 05 - ServiceDefaults)
builder.AddServiceDefaults();

// 2. Підключення баз даних та кешу (Лаб 06)
builder.AddMongoDBClient("ReviewsDb");
builder.AddRedisDistributedCache("redis"); // Підключаємо Redis через Aspire

// Реєстрація мапінгу MongoDB (має бути до ініціалізації бази даних)
QPQ.Reviews.Infrastructure.Persistence.MongoMappings.Register();

// Реєстрація шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Пряма реєстрація IReviewQueries для читання
builder.Services.AddScoped<QPQ.Reviews.Application.Interfaces.IReviewQueries, QPQ.Reviews.Infrastructure.Repositories.ReviewQueries>();

builder.Services.AddControllers();

// ==========================================
// 3. Кешування, gRPC та Метрики (Лаб 06)
// ==========================================

// Реєструємо власні метрики для кешу (Критерій 14)
builder.Services.AddSingleton<CacheMetrics>();

// Локальний кеш у пам'яті з лімітом (Критерій 3)
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);

// Додаємо підтримку gRPC з серверним інтерцептором для логування (Критерій 13)
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

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

// 4. Мапінг нових ендпоінтів (Лаб 05 та 06)
app.MapDefaultEndpoints(); // Health checks та інші ендпоінти від ServiceDefaults
app.MapGrpcService<ReviewsGrpcService>(); // Мапінг нашого gRPC сервісу

app.Run();