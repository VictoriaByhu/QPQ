using Platform.EventBus;
using RabbitMQ.Client;
using Sessions.Api.IntegrationEvents.Events;
using Sessions.Domain.Exceptions;

namespace Sessions.Api.IntegrationEvents;

/// <summary>Reads swap events from sessions.swaps-sync and keeps the AcceptedSwaps copy in sync.</summary>
public sealed class SwapEventsConsumer : RabbitMqConsumerBase
{
    public SwapEventsConsumer(IConnection connection, IServiceScopeFactory scopeFactory, ILogger<SwapEventsConsumer> logger)
        : base(connection, scopeFactory, logger)
    {
        Handle<SwapAcceptedEvent>();
        Handle<SwapCompletedEvent>();
        Handle<SwapCancelledEvent>();
    }

    protected override string QueueName => Topology.SessionsQueue;

    // Event data that breaks the domain rules (for example, the same user on both sides) will never apply.
    protected override bool IsPermanent(Exception ex) => base.IsPermanent(ex) || ex is DomainException;
}

public static class SwapEventsRegistration
{
    public static IServiceCollection AddSwapEventsConsumer(this IServiceCollection services)
    {
        services.AddScoped<AcceptedSwapReplicator>();
        services.AddIntegrationEventHandler<SwapAcceptedEvent, SwapAcceptedHandler>();
        services.AddIntegrationEventHandler<SwapCompletedEvent, SwapCompletedHandler>();
        services.AddIntegrationEventHandler<SwapCancelledEvent, SwapCancelledHandler>();
        return services;
    }
}
