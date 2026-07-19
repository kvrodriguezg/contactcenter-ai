using System.Text.Json;
using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events to the durable topic exchange as persistent JSON messages.
/// Selected only when <c>Messaging:Enabled=true</c>. Publish failures are surfaced to the caller
/// (which logs and continues), so an unhealthy broker never blocks an upload — polling reconciles.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        RabbitMqConnection connection,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class
    {
        var routingKey = MessagingRoutingKeys.Resolve(typeof(TEvent));

        if (routingKey is null)
        {
            _logger.LogWarning(
                "No hay routing key configurada para el evento {EventType}; no se publica",
                typeof(TEvent).Name);
            return;
        }

        var connection = await _connection.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await RabbitMqTopology.DeclareAsync(channel, _settings, ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, SerializerOptions);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Type = typeof(TEvent).Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Evento {EventType} publicado en {Exchange} con routing key {RoutingKey}",
            typeof(TEvent).Name,
            _settings.ExchangeName,
            routingKey);
    }
}
