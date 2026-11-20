using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.ServiceDefaults.HealthChecks;

/// <summary>Writes a health report as JSON with one entry per check. Exceptions are reduced to their message.</summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new
        {
            Status = report.Status.ToString(),
            TotalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                DurationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                entry.Value.Description,
                Error = entry.Value.Exception?.Message,
                entry.Value.Tags
            })
        };

        return JsonSerializer.SerializeAsync(context.Response.Body, body, Options, context.RequestAborted);
    }
}
