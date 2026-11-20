using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus;

/// <summary>
/// Publishes events over one channel with publisher confirms. IChannel is not thread-safe,
/// so publishes are serialized by a semaphore; the channel is recreated if the broker closed it.
/// </summary>
public sealed class RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct)
        where T : IntegrationEvent
    {
        var eventType = typeof(T).Name;
        var messageId = @event.EventId.ToString();
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonDefaults.Options);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            Type = eventType,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.SpecifyKind(@event.OccurredAt, DateTimeKind.Utc)).ToUnixTimeSeconds()),
            CorrelationId = @event.CorrelationId,
            Headers = new Dictionary<string, object?>
            {
                [MessageHeaders.CorrelationId] = @event.CorrelationId,
                [MessageHeaders.SchemaVersion] = @event.SchemaVersion
            }
        };

        using var activity = TracePropagation.StartPublishActivity(exchange, routingKey, messageId, eventType);
        TracePropagation.Inject(activity, properties);

        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, ct);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _lock.Release();
        }

        logger.LogDebug("Broker confirmed {EventType} {EventId} on {Exchange}/{RoutingKey}",
            eventType, messageId, exchange, routingKey);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
            await _channel.DisposeAsync();

        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        await Topology.DeclareAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _lock.Dispose();
    }
}
