namespace Platform.EventBus;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

/// <summary>
/// Thrown by a handler when the event can never be applied (for example, it breaks a business rule).
/// Such a message goes straight to the dead-letter queue instead of being retried.
/// </summary>
public sealed class PermanentEventException(string message, Exception? inner = null) : Exception(message, inner);
