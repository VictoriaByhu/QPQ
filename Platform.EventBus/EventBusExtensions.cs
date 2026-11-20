using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Platform.EventBus;

public static class EventBusExtensions
{
    /// <summary>
    /// Publisher plus a broker health check for /health/ready. Expects the connection registered by
    /// AddRabbitMQClient with its own health check disabled. A broker outage only delays replication,
    /// the service keeps answering its API, hence Degraded.
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddHealthChecks()
            .AddRabbitMQ(sp => sp.GetRequiredService<IConnection>(),
                name: "messaging",
                failureStatus: HealthStatus.Degraded,
                tags: ["messaging", "rabbitmq", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        return services;
    }

    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
        => services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
}
