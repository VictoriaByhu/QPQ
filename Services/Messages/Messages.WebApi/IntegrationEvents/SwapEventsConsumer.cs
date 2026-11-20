using Messages.Domain.Exceptions;
using Messages.WebApi.IntegrationEvents.Events;
using Platform.EventBus;
using RabbitMQ.Client;

namespace Messages.WebApi.IntegrationEvents;

/// <summary>Reads swap events from messages.swaps-sync and keeps swap conversations in sync.</summary>
public sealed class SwapEventsConsumer : RabbitMqConsumerBase
{
    public SwapEventsConsumer(IConnection connection, IServiceScopeFactory scopeFactory, ILogger<SwapEventsConsumer> logger)
        : base(connection, scopeFactory, logger)
    {
        Handle<SwapAcceptedEvent>();
        Handle<SwapCompletedEvent>();
        Handle<SwapCancelledEvent>();
    }

    protected override string QueueName => Topology.MessagesQueue;

    // Event data that breaks the domain rules (for example, the same user on both sides) will never apply.
    protected override bool IsPermanent(Exception ex) => base.IsPermanent(ex) || ex is DomainException;
}

public static class SwapEventsRegistration
{
    public static IServiceCollection AddSwapEventsConsumer(this IServiceCollection services)
    {
        services.AddScoped<ConversationReplicator>();
        services.AddIntegrationEventHandler<SwapAcceptedEvent, SwapAcceptedHandler>();
        services.AddIntegrationEventHandler<SwapCompletedEvent, SwapCompletedHandler>();
        services.AddIntegrationEventHandler<SwapCancelledEvent, SwapCancelledHandler>();
        return services;
    }
}
