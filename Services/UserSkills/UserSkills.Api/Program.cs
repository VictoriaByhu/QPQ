using System.Reflection;
using Microsoft.OpenApi.Models;
using Platform.Caching;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Serilog;
using UserSkills.Api.Grpc;
using UserSkills.Api.Middleware;
using UserSkills.Api.Services;
using UserSkills.Bll.Caching;
using UserSkills.Bll.Mapping;
using UserSkills.Bll.Services;
using UserSkills.Dal;
using UserSkills.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Рядок підключення береться з конфігурації: Aspire передає його автоматично (ресурс UserSkillsDb),
// для запуску без Aspire — User Secrets або змінна оточення ConnectionStrings__UserSkillsDb.
var connectionString = builder.Configuration.GetConnectionString("UserSkillsDb")
    ?? throw new InvalidOperationException("Connection string 'UserSkillsDb' is missing");

// Логування (Serilog), трасування (OpenTelemetry), health checks і service discovery — зі спільного ServiceDefaults.
builder.AddServiceDefaults();

// gRPC-сервер (Лаб. 06): AddGrpc + LoggingInterceptor і окремий HTTP/2-endpoint, порт якого дає AppHost (GRPC_PORT).
builder.AddGrpcServerDefaults();

// Кешування (Лаб. 06): L1 IMemoryCache + L2 Redis зі спільної бібліотеки Platform.Caching (ресурс "redis" в AppHost).
builder.AddTwoLevelCache("UserSkills");

// Scoped: один Unit of Work (одне з'єднання) на HTTP-запит.
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
// Сервіси обгорнуті декораторами з кешем: контролери й gRPC залежать від інтерфейсів і не знають про кеш.
builder.Services.AddScoped<UserSkillService>();
builder.Services.AddScoped<IUserSkillService>(sp =>
    new CachedUserSkillService(sp.GetRequiredService<UserSkillService>(), sp.GetRequiredService<ITwoLevelCache>()));
builder.Services.AddScoped<ReferenceDataService>();
builder.Services.AddScoped<IReferenceDataService>(sp =>
    new CachedReferenceDataService(sp.GetRequiredService<ReferenceDataService>(), sp.GetRequiredService<ITwoLevelCache>()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<UserSkillProfile>();
});

builder.Services.AddControllers();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QPQ UserSkills API",
        Version = "v1",
        Description = "Skills subdomain: user skills (offers and wants), languages and availability."
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
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<UserSkillsGrpcService>();    // gRPC (HTTP/2) для Aggregator; REST лишається для зовнішніх клієнтів
app.MapDefaultEndpoints();
app.MapCacheHealth();                           // GET /health/cache: hits/misses/hitRatio за рівнями

app.Run();
