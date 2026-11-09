using System.Reflection;
using Catalog.Api.Caching;
using Catalog.Api.Grpc;
using Catalog.Api.Middleware;
using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using Catalog.Dal.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Рядок підключення: appsettings.json (LocalDB) -> User Secrets (Development) ->
// Aspire (ресурс CatalogDb) -> змінна оточення ConnectionStrings__CatalogDb.
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

// Логування (Serilog), трасування (OpenTelemetry), health checks і service discovery — зі спільного ServiceDefaults.
// SQL-команди EF Core вмикаються в appsettings.Development.json (Serilog:MinimumLevel:Override).
builder.AddServiceDefaults();

// gRPC-сервер (Лаб. 06): AddGrpc + LoggingInterceptor і окремий HTTP/2-endpoint, порт якого дає AppHost (GRPC_PORT).
builder.AddGrpcServerDefaults();

// DAL: DbContext і Unit of Work (один контекст на HTTP-запит).
builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Кешування (Лаб. 06). L1 — IMemoryCache з обмеженням розміру (кожен запис має Size, найстаріші витісняються
// за пріоритетом і терміном життя); L2 — Redis через Aspire (ресурс "redis" в AppHost -> ConnectionStrings:redis).
// Без рядка підключення (запуск поза Aspire, тести) L2 замінює локальний IDistributedCache у пам'яті.
builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 10_000;                       // у «одиницях» Size записів
    o.TrackStatistics = true;                   // для метрик розміру L1 і health check
});
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
    builder.AddRedisDistributedCache("redis");
else
    builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

// BLL: сервіси, AutoMapper і валідатори FluentValidation (викликаються явно в контролерах).
// Кожен сервіс обгорнутий декоратором з кешем: контролери й gRPC залежать від інтерфейсу й не знають про кеш.
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<ISkillService>(sp =>
    new CachedSkillService(sp.GetRequiredService<SkillService>(), sp.GetRequiredService<ITwoLevelCache>()));
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ICategoryService>(sp =>
    new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<ITwoLevelCache>()));
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<ITagService>(sp =>
    new CachedTagService(sp.GetRequiredService<TagService>(), sp.GetRequiredService<ITwoLevelCache>()));

builder.Services.AddHostedService<CacheWarmupService>();
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");   // статистика кешу в /health

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<CatalogProfile>();
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateSkillDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QPQ Catalog API",
        Version = "v1",
        Description = "Skills subdomain: skills reference book, categories, levels and tags (EF Core, Lab 03)."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

// Міграції та сідінг під час старту. Сідінг вимикається в Production або через Database:Seed=false.
if (app.Configuration.GetValue("Database:Migrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    if (db.Database.IsRelational()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();                      // тести на InMemory

    if (!app.Environment.IsProduction() && app.Configuration.GetValue("Database:Seed", true))
        await DbInitializer.SeedAsync(db);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestContextMiddleware>();
app.UseSerilogRequestLogging(o =>
    o.EnrichDiagnosticContext = (diag, http) => diag.Set("RequestPath", http.Request.Path.Value));
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();       // gRPC (HTTP/2) для Aggregator; REST лишається для зовнішніх клієнтів
app.MapDefaultEndpoints();

app.Run();

// Потрібно інтеграційним тестам (WebApplicationFactory<Program>).
public partial class Program;
