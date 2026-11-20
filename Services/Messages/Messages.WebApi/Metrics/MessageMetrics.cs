using System.Diagnostics.Metrics;
using Platform.ServiceDefaults.Metrics;

namespace Messages.WebApi.Metrics;

/// <summary>Business metrics of the Messages service. Tags are low-cardinality only: no user, conversation or message ids.</summary>
public sealed class MessageMetrics
{
    public const string MeterName = "Messages.WebApi";

    private const string TypeTag = "message.type";

    private readonly Counter<long> _sent;

    public MessageMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);

        _sent = meter.CreateCounter<long>("messages.sent", unit: "{message}",
            description: "Messages sent to swap conversations, by message type.");
        Duration = meter.CreateDurationHistogram("messages.operation.duration",
            "Duration of message operations, by operation and outcome.");
    }

    public Histogram<double> Duration { get; }

    public void Sent(string messageType) => _sent.Add(1, new KeyValuePair<string, object?>(TypeTag, messageType));
}
