using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Platform.ServiceDefaults.Correlation;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Спільні налаштування платформи: Serilog (структуровані логи), OpenTelemetry (трасування й метрики),
/// health checks, service discovery, resilience для HttpClient і CorrelationId.
/// Підключається в кожному сервісі двома викликами: AddServiceDefaults() і MapDefaultEndpoints().
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddSerilogDefaults();
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // retry, timeout, circuit breaker (детально — Лаб. 07). Короткі таймаути, щоб недоступний сервіс
            // швидко перетворювався на попередження Aggregator, а не на довге очікування клієнта.
            http.AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 1;
                o.Retry.Delay = TimeSpan.FromMilliseconds(200);
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);   // не менше 2 × AttemptTimeout
            });
            http.AddServiceDiscovery();                        // http://orders-api -> реальна адреса від Aspire
            http.AddHttpMessageHandler<CorrelationIdHandler>(); // CorrelationId у вихідних запитах
        });

        return builder;
    }

    /// <summary>Serilog як основний провайдер: enrichers, LogContext, JSON у консоль і передача в OpenTelemetry.</summary>
    public static TBuilder AddSerilogDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var serviceName = builder.Environment.ApplicationName;

        builder.Logging.ClearProviders();                  // єдиний вивід — Serilog (без дубля стандартного консольного логера)

        builder.Services.AddSerilog((services, cfg) => cfg
                .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Polly", LogEventLevel.Warning)   // службові події resilience — лише попередження
                // Дрібні налаштування конкретного сервісу (наприклад, SQL-команди EF Core) можуть
                // перевизначити рівні в секції Serilog його appsettings — вони читаються останніми.
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()                       // CorrelationId та інші властивості запиту
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithSpan()                             // TraceId, SpanId, ParentId
                .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true);                           // -> OpenTelemetry -> Structured logs у Dashboard

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(o =>
        {
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(t =>
            {
                // 100 % у Development, налаштований відсоток у Production (Telemetry:SamplingRatio).
                var ratio = builder.Environment.IsDevelopment() ? 1.0 : builder.Configuration.GetValue("Telemetry:SamplingRatio", 0.1);
                t.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)));

                t.AddSource(builder.Environment.ApplicationName)                // власні spans (Activity API)
                    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources") // MongoDB
                    .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                        !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();                              // Dapper і EF Core
            });

        // OTLP-адресу задає Aspire (OTEL_EXPORTER_OTLP_ENDPOINT); без неї експорт вимкнено.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>Службові endpoints /health (усі перевірки) та /alive (лише liveness).</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
        return app;
    }
}
