using System.Reflection;
using Microsoft.OpenApi.Models;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;
using Swaps.Api.Grpc;
using Swaps.Api.HealthChecks;
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
builder.Services.AddScoped<MeteredSwapService>();
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
// grpc.health.v1 over the same health checks, so the aggregator can probe this service.
builder.Services.AddGrpcHealthChecks();

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
