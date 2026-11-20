using System.Text.Json.Serialization;
using MediatR;
using Messages.WebApi.HealthChecks;
using Messages.WebApi.IntegrationEvents;
using Platform.EventBus;
using Messages.WebApi.Metrics;
using Microsoft.OpenApi.Models;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;
using Messages.Application;
using Messages.Infrastructure;
using Messages.WebApi.Grpc;
using Messages.WebApi.Middleware;
using Messages.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.AddMessagesHealthChecks();

// Our own broker check (tagged ready, Degraded) and tracing spans replace the integration's ones.
builder.AddRabbitMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = true;
});
builder.Services.AddEventBus();
builder.Services.AddSwapEventsConsumer();
builder.Services.AddHostedService<SwapEventsConsumer>();

builder.Services.AddSingleton<MessageMetrics>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MessageMetricsBehavior<,>));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddGrpc(o => o.Interceptors.Add<GrpcLoggingInterceptor>());
// grpc.health.v1 for the aggregator: serving as long as the database answers.
builder.Services.AddGrpcHealthChecks(o => o.Services.Map("", check => check.Tags.Contains("database")));

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QPQ Messages API",
        Version = "v1",
        Description = "Swaps subdomain: conversations of accepted swaps, messages and attachments (Clean Architecture, CQRS, MongoDB)."
    });

    options.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = HttpContextCurrentUser.UserIdHeader,
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
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LogContextMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<MessagesGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

await app.RunAsync();
