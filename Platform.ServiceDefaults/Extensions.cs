using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // 1. Налаштування Serilog замість стандартного логера
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                // Логування в консоль у форматі Compact JSON
                .WriteTo.Console(new CompactJsonFormatter());
        }, writeToProviders: true); // Передає логи в OpenTelemetry Aspire

        // 2. Додаємо HttpContextAccessor для роботи з заголовками (CorrelationId)
        builder.Services.AddHttpContextAccessor();

        // 3. Реєструємо наш DelegatingHandler для вихідних HTTP-запитів
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();

            // Додаємо наскрізну передачу CorrelationId у всі вихідні виклики
            http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Трасування SQL Server
                    .AddSqlClientInstrumentation()
                    // Трасування MongoDB
                    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                    // Трасування власних бізнес-операцій
                    .AddSource("BusinessActivitySource");
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

// 4. Реалізація DelegatingHandler для наскрізного CorrelationId
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains("X-Correlation-Id"))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}