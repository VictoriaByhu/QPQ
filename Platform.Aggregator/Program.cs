using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddTransient<UserIdForwardingHandler>();

builder.Services.AddHttpClient<SwapsClient>(c => c.BaseAddress = new("https+http://swaps-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>();
builder.Services.AddHttpClient<SessionsClient>(c => c.BaseAddress = new("https+http://sessions-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>();
builder.Services.AddHttpClient<MessagesClient>(c => c.BaseAddress = new("https+http://messages-api"))
    .AddHttpMessageHandler<UserIdForwardingHandler>();

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
