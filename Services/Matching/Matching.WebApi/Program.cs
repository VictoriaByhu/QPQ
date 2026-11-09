using System.Reflection;
using Matching.Application;
using Matching.Infrastructure;
using Matching.WebApi.Mapping;
using Matching.WebApi.Middleware;
using Microsoft.OpenApi.Models;
using Platform.ServiceDefaults.Correlation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Логування (Serilog), трасування (OpenTelemetry), health checks і service discovery — зі спільного ServiceDefaults.
builder.AddServiceDefaults();

// Кожен шар надає власний метод розширення.
builder.Services
    .AddApplication(builder.Configuration)         // MediatR, behaviors, валідатори
    .AddInfrastructure(builder.Configuration);     // MongoDB, репозиторії, індекси, сідінг

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<MatchingProfile>();
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks().AddMongoDb(name: "mongodb");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QPQ Matching API",
        Version = "v1",
        Description = "Skills subdomain: pair matching read model on MongoDB (Clean Architecture, CQRS, MediatR, Lab 04)."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
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
app.MapDefaultEndpoints();                       // /health (разом із перевіркою MongoDB) і /alive

app.Run();

// Потрібно інтеграційним тестам (WebApplicationFactory<Program>).
public partial class Program;
