using System.Text.Json;
using System.Text.Json.Serialization;
using Messages.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Resilience;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Sessions.Grpc;
using Swaps.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// The integration's own Redis check reports Unhealthy; ours reports Degraded, since Redis is only a cache.
builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Connection string 'redis' is missing");
builder.Services.AddAggregatorHealthChecks(redisConnectionString);

builder.Services.AddSingleton<AggregatorCacheMetrics>();
builder.Services.AddSingleton<FullSwapCache>();

builder.Services.AddTransient<UserIdForwardingHandler>();

builder.Services.AddGrpcClient<SwapsService.SwapsServiceClient>(o => o.Address = new Uri("https://swaps-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>()
    .WithResilience(ResiliencePolicies.Swaps)
    .WithGrpcRetry(ResiliencePolicies.Swaps);
builder.Services.AddGrpcClient<SessionsService.SessionsServiceClient>(o => o.Address = new Uri("https://sessions-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>()
    .WithResilience(ResiliencePolicies.Sessions)
    .WithGrpcRetry(ResiliencePolicies.Sessions);
builder.Services.AddGrpcClient<MessagesService.MessagesServiceClient>(o => o.Address = new Uri("https://messages-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>()
    .WithResilience(ResiliencePolicies.Messages)
    .WithGrpcRetry(ResiliencePolicies.Messages);

builder.Services.AddScoped<FullSwapService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
