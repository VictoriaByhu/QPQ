using System.Diagnostics;
using System.Diagnostics.Metrics;
using Platform.ServiceDefaults.Correlation;
using Serilog;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

// Логування, трасування, health checks, service discovery — зі спільного ServiceDefaults.
builder.AddServiceDefaults();

// YARP: маршрути й кластери — у секції ReverseProxy (appsettings.json), адреси — логічні імена сервісів.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();     // http://catalog-api -> реальна адреса від Aspire

// Власні метрики Gateway: запити, помилки, тривалість (запити за секунду / середній час / частка помилок).
var meter = new Meter("Platform.ApiGateway");
var requests = meter.CreateCounter<long>("gateway.requests", description: "Кількість запитів через Gateway");
var errors = meter.CreateCounter<long>("gateway.errors", description: "Запити зі статусом 5xx або помилкою проксування");
var duration = meter.CreateHistogram<double>("gateway.request.duration", "ms", "Тривалість проксування");
builder.Services.AddOpenTelemetry().WithMetrics(m => m.AddMeter(meter.Name));

var app = builder.Build();

// CorrelationId генерується тут першим (до YARP): якщо клієнт надіслав X-Correlation-Id — зберігається.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(o =>
    o.EnrichDiagnosticContext = (diag, http) => diag.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString()));

app.MapReverseProxy(proxy =>
{
    // Виконується в конвеєрі YARP — тут уже відомі маршрут, кластер і призначення.
    proxy.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.Routing");
        var sw = Stopwatch.StartNew();
        await next();
        sw.Stop();

        var feature = context.GetReverseProxyFeature();
        var route = context.GetRouteModel()?.Config.RouteId ?? "unknown";
        var cluster = feature.Cluster.Config.ClusterId;
        var destination = feature.ProxiedDestination?.Model.Config.Address ?? "n/a";
        var error = context.Features.Get<IForwarderErrorFeature>();
        var status = context.Response.StatusCode;
        var tags = new KeyValuePair<string, object?>[] { new("route", route), new("cluster", cluster) };

        requests.Add(1, tags);
        duration.Record(sw.Elapsed.TotalMilliseconds, tags);

        if (error is not null)
        {
            errors.Add(1, tags);
            logger.LogError(error.Exception,
                "{Method} {Path} -> route {Route}, cluster {Cluster} failed: {ForwarderError} (status {StatusCode}) in {Elapsed} ms",
                context.Request.Method, context.Request.Path, route, cluster, error.Error, status, sw.ElapsedMilliseconds);
        }
        else
        {
            if (status >= 500) errors.Add(1, tags);
            logger.LogInformation(
                "{Method} {Path} -> route {Route}, cluster {Cluster}, destination {Destination}: {StatusCode}, {ResponseSize} bytes in {Elapsed} ms",
                context.Request.Method, context.Request.Path, route, cluster, destination,
                status, context.Response.ContentLength ?? -1, sw.ElapsedMilliseconds);
        }
    });
});
app.MapDefaultEndpoints();

app.Run();
