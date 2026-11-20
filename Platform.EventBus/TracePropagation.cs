using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

/// <summary>
/// Carries the W3C trace context (traceparent, tracestate) in message headers, so one trace spans
/// the HTTP request that changed the data, the publish and the processing in every consumer.
/// </summary>
public static class TracePropagation
{
    public const string SourceName = "Platform.Messaging";

    public static readonly ActivitySource Source = new(SourceName);

    private static TextMapPropagator Propagator => Propagators.DefaultTextMapPropagator;

    public static Activity? StartPublishActivity(string exchange, string routingKey, string messageId, string eventType)
    {
        var activity = Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", messageId);
        activity?.SetTag("messaging.message.type", eventType);
        return activity;
    }

    public static void Inject(Activity? activity, BasicProperties properties)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        var context = activity?.Context ?? Activity.Current?.Context ?? default;
        Propagator.Inject(new PropagationContext(context, Baggage.Current), properties.Headers,
            static (headers, key, value) => headers[key] = value);
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var parent = Propagator.Extract(default, ea.BasicProperties.Headers, ExtractHeader);
        Baggage.Current = parent.Baggage;

        var activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", ea.RoutingKey);
        activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        activity?.SetTag("messaging.message.type", ea.BasicProperties.Type);
        return activity;
    }

    // Header values set by a client come back as UTF-8 byte arrays.
    private static IEnumerable<string> ExtractHeader(IDictionary<string, object?>? headers, string key)
        => MessageHeaders.GetString(headers, key) is { } value ? [value] : [];
}
