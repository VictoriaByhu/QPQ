using System.Reflection;
using Microsoft.OpenApi.Models;
using Platform.EventBus;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;
using Swaps.Api.Grpc;
using Swaps.Api.HealthChecks;
using Swaps.Api.IntegrationEvents;
using Swaps.Api.Metrics;
using Swaps.Api.Middleware;
using Swaps.Api.Services;
using Swaps.Bll.Caching;
using Swaps.Bll.Mapping;
using Swaps.Bll.Services;
using Swaps.Dal;
using Swaps.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// The integration's own Redis check reports Unhealthy; ours below reports Degraded, since Redis is only a cache.
builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

var connectionString = builder.Configuration.GetConnectionString("SwapsDb")
    ?? throw new InvalidOperationException("Connection string 'SwapsDb' is missing");
var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Connection string 'redis' is missing");

builder.Services.AddSwapsHealthChecks(connectionString, redisConnectionString);

// Our own broker check (tagged ready, Degraded) and tracing spans replace the integration's ones.
builder.AddRabbitMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = true;
});
builder.Services.AddEventBus();

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 10_000;
    o.TrackStatistics = true;
});
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<TwoLevelCache>();
builder.Services.AddSingleton<SwapMetrics>();

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<SwapService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped(sp =>
    ActivatorUtilities.CreateInstance<CachedSwapService>(sp, (ISwapService)sp.GetRequiredService<SwapService>()));
// Decorators, outermost first: Metered → Publishing → Cached → SwapService.
// Publishing sits above the cache, so the card it reads after a change is the committed one;
// metrics wrap the whole operation, publishing included.
builder.Services.AddScoped(sp => new PublishingSwapService(
    sp.GetRequiredService<CachedSwapService>(),
    sp.GetRequiredService<CachedSwapService>(),
    sp.GetRequiredService<IEventPublisher>(),
    sp.GetRequiredService<IHttpContextAccessor>(),
    sp.GetRequiredService<ILogger<PublishingSwapService>>()));
builder.Services.AddScoped(sp => new MeteredSwapService(
    sp.GetRequiredService<PublishingSwapService>(),
    sp.GetRequiredService<CachedSwapService>(),
    sp.GetRequiredService<SwapMetrics>()));
builder.Services.AddScoped<ISwapService>(sp => sp.GetRequiredService<MeteredSwapService>());
builder.Services.AddScoped<ISwapCardReader>(sp => sp.GetRequiredService<MeteredSwapService>());
builder.Services.AddScoped<ISkillService>(sp =>
    ActivatorUtilities.CreateInstance<CachedSkillService>(sp, (ISkillService)sp.GetRequiredService<SkillService>()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<SwapProfile>();
});

builder.Services.AddControllers();
builder.Services.AddGrpc(o => o.Interceptors.Add<GrpcLoggingInterceptor>());
// grpc.health.v1 for the aggregator: serving as long as the database answers. The Redis check is left out,
// since a cache outage only degrades the service and its timeout would outlast the caller's probe deadline.
builder.Services.AddGrpcHealthChecks(o => o.Services.Map("", check => check.Tags.Contains("database")));

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QPQ Swaps API",
        Version = "v1",
        Description = "Swaps subdomain: swaps, their skills and status history."
    });

    options.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-User-Id",
        Description = "Development only: GUID of the current user (UserId from Identity)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "UserId" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<SwapsGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
