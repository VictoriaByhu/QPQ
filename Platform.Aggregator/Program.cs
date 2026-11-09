using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults.Correlation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Логування, трасування, health checks, service discovery, CorrelationIdHandler для HttpClient.
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource("Platform.Aggregator"));

// Typed HttpClients: адреса — логічне ім'я сервісу, реальну адресу підставляє service discovery.
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddHttpClient<UserSkillsClient>(c => c.BaseAddress = new Uri("https+http://userskills-api"));
builder.Services.AddHttpClient<MatchingClient>(c => c.BaseAddress = new Uri("https+http://matching-api"));
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
