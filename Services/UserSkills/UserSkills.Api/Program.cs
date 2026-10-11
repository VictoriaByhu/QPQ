using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using UserSkills.Api.Middleware;
using UserSkills.Api.Services;
using UserSkills.Bll.Mapping;
using UserSkills.Bll.Services;
using UserSkills.Dal;
using UserSkills.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Рядок підключення береться з конфігурації: Aspire передає його автоматично (ресурс UserSkillsDb),
// для запуску без Aspire — User Secrets або змінна оточення ConnectionStrings__UserSkillsDb.
var connectionString = builder.Configuration.GetConnectionString("UserSkillsDb")
    ?? throw new InvalidOperationException("Connection string 'UserSkillsDb' is missing");

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Scoped: один Unit of Work (одне з'єднання) на HTTP-запит.
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IUserSkillService, UserSkillService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();

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

app.Use(async (context, next) =>
{
    // Кореляційний ідентифікатор у кожному записі логу; секрети (токени, паролі) не логуються.
    using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
    {
        await next();
    }
});

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
