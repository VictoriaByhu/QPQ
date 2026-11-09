using System.Text.Json.Serialization;
using Catalog.GrpcContracts;
using Matching.GrpcContracts;
using Platform.Aggregator.Services;
using UserSkills.GrpcContracts;
using Platform.ServiceDefaults.Correlation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Логування, трасування, health checks, service discovery, CorrelationIdHandler для HttpClient.
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource("Platform.Aggregator"));

// Типізовані gRPC-клієнти (Лаб. 06). Адреса — логічне ім'я сервісу, реальну адресу підставляє service discovery;
// "_grpc." вибирає іменований endpoint "grpc" (HTTP/2) з AppHost, а не звичайний REST-endpoint.
// Клієнти створює та сама фабрика HttpClient, тому ServiceDefaults (service discovery, resilience, CorrelationId
// у метаданих виклику) діють і для gRPC; канал повторно використовує одне HTTP/2-з'єднання.
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("http://_grpc.catalog-api"));
builder.Services.AddGrpcClient<UserSkillsService.UserSkillsServiceClient>(o => o.Address = new Uri("http://_grpc.userskills-api"));
builder.Services.AddGrpcClient<MatchingService.MatchingServiceClient>(o => o.Address = new Uri("http://_grpc.matching-api"));

// Redis (ресурс "redis" з AppHost) — кеш зібраних відповідей. Без нього (запуск поза Aspire) — кеш у пам'яті процесу.
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
    builder.AddRedisDistributedCache("redis");
else
    builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<AggregateCache>();
builder.Services.AddScoped<ISkillAggregationService, SkillAggregationService>();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);  // camelCase, без null
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
