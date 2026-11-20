using RabbitMQ.Client;

namespace Platform.EventBus;

/// <summary>
/// Broker topology of the swap events. Declarations are idempotent, so the publisher and every consumer
/// declare everything at startup and whichever starts first creates it.
/// </summary>
public static class Topology
{
    /// <summary>Topic exchange the Swaps service publishes to; routing keys are swap.accepted, swap.completed, swap.cancelled.</summary>
    public const string SwapsExchange = "swaps-events";

    /// <summary>
    /// Direct dead-letter exchange: each queue dead-letters with its own name as the routing key,
    /// so a rejected message lands only in the DLQ of the queue that rejected it.
    /// </summary>
    public const string SwapsDeadLetterExchange = "swaps-events.dlx";

    public const string SwapEventsBinding = "swap.*";

    public const string SessionsQueue = "sessions.swaps-sync";
    public const string MessagesQueue = "messages.swaps-sync";

    public static readonly IReadOnlyList<string> SwapsConsumerQueues = [SessionsQueue, MessagesQueue];

    public static string DeadLetterQueue(string queue) => $"{queue}.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct = default)
    {
        await channel.ExchangeDeclareAsync(SwapsExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, arguments: null, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(SwapsDeadLetterExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, arguments: null, cancellationToken: ct);

        foreach (var queue in SwapsConsumerQueues)
        {
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-dead-letter-exchange"] = SwapsDeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queue
                },
                cancellationToken: ct);
            await channel.QueueBindAsync(queue, SwapsExchange, SwapEventsBinding, cancellationToken: ct);

            var deadLetterQueue = DeadLetterQueue(queue);
            await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: ct);
            await channel.QueueBindAsync(deadLetterQueue, SwapsDeadLetterExchange, queue, cancellationToken: ct);
        }
    }
}
