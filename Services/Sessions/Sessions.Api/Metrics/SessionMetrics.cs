using System.Diagnostics.Metrics;
using Platform.ServiceDefaults.Metrics;

namespace Sessions.Api.Metrics;

/// <summary>Business metrics of the Sessions service. Tags are low-cardinality only: no user, session or swap ids.</summary>
public sealed class SessionMetrics
{
    public const string MeterName = "Sessions.Api";

    private const string FormatTag = "session.format";

    private readonly Counter<long> _created;
    private readonly Counter<long> _rescheduled;

    public SessionMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);

        _created = meter.CreateCounter<long>("sessions.created", unit: "{session}",
            description: "Sessions scheduled, by format.");
        _rescheduled = meter.CreateCounter<long>("sessions.rescheduled", unit: "{session}",
            description: "Sessions moved to another time or place, by format.");
        Duration = meter.CreateDurationHistogram("sessions.operation.duration",
            "Duration of session operations, by operation and outcome.");
    }

    public Histogram<double> Duration { get; }

    public void Created(string format) => _created.Add(1, new KeyValuePair<string, object?>(FormatTag, format));

    public void Rescheduled(string format) => _rescheduled.Add(1, new KeyValuePair<string, object?>(FormatTag, format));
}
