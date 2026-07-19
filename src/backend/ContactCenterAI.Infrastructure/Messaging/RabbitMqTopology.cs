using RabbitMQ.Client;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// Declares the durable topic exchange, durable queues and their bindings. Declarations are
/// idempotent, so this is safe to call from both the publisher and every consumer on startup.
/// </summary>
public static class RabbitMqTopology
{
    public static async Task DeclareAsync(
        IChannel channel,
        RabbitMqSettings settings,
        CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(
            exchange: settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await DeclareBoundQueueAsync(
            channel,
            settings.ExchangeName,
            settings.DocumentProcessingQueue,
            MessagingRoutingKeys.DocumentUploaded,
            cancellationToken);

        await DeclareBoundQueueAsync(
            channel,
            settings.ExchangeName,
            settings.TicketEscalationQueue,
            MessagingRoutingKeys.TicketCreated,
            cancellationToken);
    }

    private static async Task DeclareBoundQueueAsync(
        IChannel channel,
        string exchange,
        string queue,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: routingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
