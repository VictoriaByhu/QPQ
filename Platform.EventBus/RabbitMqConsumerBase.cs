using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace Platform.EventBus;

/// <summary>
/// Consumes one queue with manual acknowledgements. Each message is routed by its Type property to the handler
/// of that event, inside its own DI scope.
/// <list type="bullet">
/// <item>success → ack;</item>
/// <item>permanent failure (bad JSON, unknown type or schema version, broken business rule) → nack without requeue, the message goes to the queue's DLQ;</item>
/// <item>transient failure → republished to the same queue with x-retry-count + 1 after a 1·2ⁿ s pause, the original is acked;
/// after <see cref="MaxRetries"/> retries → DLQ.</item>
/// </list>
/// </summary>
public abstract class RabbitMqConsumerBase(IConnection connection, IServiceScopeFactory scopeFactory, ILogger logger)
    : BackgroundService
{
    public const int MaxRetries = 3;
    public const int SupportedSchemaVersion = 1;
    private const ushort PrefetchCount = 10;

    private readonly Dictionary<string, Func<IServiceProvider, byte[], CancellationToken, Task>> _handlers =
        new(StringComparer.Ordinal);

    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected abstract string QueueName { get; }

    /// <summary>Routes messages whose Type equals the event's class name to its <see cref="IIntegrationEventHandler{TEvent}"/>.</summary>
    protected void Handle<TEvent>() where TEvent : IntegrationEvent
        => _handlers[typeof(TEvent).Name] = async (services, body, ct) =>
        {
            var @event = JsonSerializer.Deserialize<TEvent>(body, JsonDefaults.Options)
                         ?? throw new PermanentEventException("The message body is empty.");
            await services.GetRequiredService<IIntegrationEventHandler<TEvent>>().HandleAsync(@event, ct);
        };

    /// <summary>Failures that a retry cannot fix. Services add their own domain exceptions.</summary>
    protected virtual bool IsPermanent(Exception ex) => ex is PermanentEventException or JsonException;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await Topology.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;
        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consuming {Queue} for {EventTypes}", QueueName, string.Join(", ", _handlers.Keys));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Stopped consuming {Queue}", QueueName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        // The body buffer is only valid during this callback, and a retry republishes it.
        var body = ea.Body.ToArray();
        var headers = ea.BasicProperties.Headers;
        var eventType = ea.BasicProperties.Type ?? "unknown";
        var eventId = ea.BasicProperties.MessageId;
        var correlationId = MessageHeaders.GetString(headers, MessageHeaders.CorrelationId);
        var attempt = MessageHeaders.GetInt(headers, MessageHeaders.RetryCount, 0);

        using var activity = TracePropagation.StartConsumerActivity(ea, QueueName);
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("messaging.rabbitmq.retry_count", attempt);

        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var eventIdScope = LogContext.PushProperty("EventId", eventId);
        using var eventTypeScope = LogContext.PushProperty("EventType", eventType);
        using var queueScope = LogContext.PushProperty("Queue", QueueName);
        using var attemptScope = LogContext.PushProperty("Attempt", attempt);

        try
        {
            if (!_handlers.TryGetValue(eventType, out var handle))
                throw new PermanentEventException($"Unknown event type '{eventType}'.");

            var schemaVersion = MessageHeaders.GetInt(headers, MessageHeaders.SchemaVersion, SupportedSchemaVersion);
            if (schemaVersion != SupportedSchemaVersion)
                throw new PermanentEventException($"Unsupported schema version {schemaVersion}.");

            await using (var scope = scopeFactory.CreateAsyncScope())
                await handle(scope.ServiceProvider, body, _stoppingToken);

            await Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            logger.LogInformation("Acked {EventType} {EventId} from {Queue}, correlation {CorrelationId}",
                eventType, eventId, QueueName, correlationId);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            // Shutdown, not a failure: the broker hands the message to the next consumer.
            await TryNackAsync(ea.DeliveryTag, requeue: true);
            logger.LogInformation("Returned {EventType} {EventId} to {Queue} on shutdown", eventType, eventId, QueueName);
        }
        catch (Exception ex) when (IsPermanent(ex))
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await TryNackAsync(ea.DeliveryTag, requeue: false);
            logger.LogError(ex, "Dead-lettered {EventType} {EventId} from {Queue} to {DeadLetterQueue}: {Reason}, correlation {CorrelationId}",
                eventType, eventId, QueueName, Topology.DeadLetterQueue(QueueName), ex.Message, correlationId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await RetryOrDeadLetterAsync(ea, body, attempt, ex, eventType, eventId, correlationId);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        BasicDeliverEventArgs ea, byte[] body, int attempt, Exception ex, string eventType, string? eventId, string? correlationId)
    {
        if (attempt >= MaxRetries)
        {
            await TryNackAsync(ea.DeliveryTag, requeue: false);
            logger.LogError(ex, "Dead-lettered {EventType} {EventId} from {Queue} to {DeadLetterQueue} after {Attempt}/{MaxRetries} retries, correlation {CorrelationId}",
                eventType, eventId, QueueName, Topology.DeadLetterQueue(QueueName), attempt, MaxRetries, correlationId);
            return;
        }

        var next = attempt + 1;
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        logger.LogWarning(ex, "Retry {Attempt}/{MaxRetries} for {EventType} {EventId} from {Queue} in {Delay}, correlation {CorrelationId}",
            next, MaxRetries, eventType, eventId, QueueName, delay, correlationId);

        try
        {
            await Task.Delay(delay, _stoppingToken);

            // Back to this queue only, through the default exchange: the other consumer already has its own copy.
            var properties = new BasicProperties(ea.BasicProperties)
            {
                Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
                {
                    [MessageHeaders.RetryCount] = next
                }
            };
            await Channel.BasicPublishAsync(string.Empty, QueueName, mandatory: false, properties, body, _stoppingToken);
            await Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            await TryNackAsync(ea.DeliveryTag, requeue: true);
        }
    }

    private async Task TryNackAsync(ulong deliveryTag, bool requeue)
    {
        try
        {
            await Channel.BasicNackAsync(deliveryTag, multiple: false, requeue);
        }
        catch (Exception ex)
        {
            // An unacknowledged message is redelivered once the channel recovers, so nothing is lost.
            logger.LogError(ex, "Failed to nack delivery {DeliveryTag} on {Queue}", deliveryTag, QueueName);
        }
    }

    private IChannel Channel => _channel ?? throw new InvalidOperationException("The consumer has not started.");
}
