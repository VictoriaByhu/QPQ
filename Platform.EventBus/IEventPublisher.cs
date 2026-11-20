namespace Platform.EventBus;

public interface IEventPublisher
{
    /// <summary>Publishes a persistent message and returns once the broker has confirmed it.</summary>
    Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct) where T : IntegrationEvent;
}
