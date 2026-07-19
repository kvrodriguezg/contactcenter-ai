using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ContactCenterAI.Infrastructure.Messaging.Consumers;

/// <summary>
/// Base hosted service for a single durable queue. Handles: initial-connect retry (broker not up
/// yet), automatic recovery of transient drops (delegated to the client), manual ack/nack, bounded
/// per-message retries, and full isolation so a poison message never stops the worker.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected RabbitMqConsumerBase(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        RabbitMqSettings settings,
        ILogger logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        Settings = settings;
        _logger = logger;
    }

    protected RabbitMqSettings Settings { get; }

    protected abstract string QueueName { get; }

    /// <summary>Processes the deserialized message body. Throw to trigger a retry.</summary>
    protected abstract Task HandleMessageAsync(
        string messageJson,
        IServiceProvider services,
        CancellationToken cancellationToken);

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SubscribeAsync(stoppingToken);

                // Stay alive; the client handles transient connection recovery underneath.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "No se pudo suscribir el consumidor a la cola {Queue}; reintentando en {Delay}s",
                    QueueName,
                    Settings.ReconnectDelaySeconds);

                await SafeCloseChannelAsync();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Settings.ReconnectDelaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        await SafeCloseChannelAsync();
    }

    private async Task SubscribeAsync(CancellationToken stoppingToken)
    {
        var connection = await _connection.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(channel, Settings, stoppingToken);
        await channel.BasicQosAsync(0, Settings.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => OnMessageReceivedAsync(channel, args, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _channel = channel;

        _logger.LogInformation("Consumidor suscrito a la cola {Queue}", QueueName);
    }

    private async Task OnMessageReceivedAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        CancellationToken stoppingToken)
    {
        string messageJson;
        try
        {
            messageJson = Encoding.UTF8.GetString(args.Body.Span);
        }
        catch (Exception exception)
        {
            // Undecodable payload: reject without requeue to avoid a poison loop.
            _logger.LogError(exception, "Mensaje ilegible en la cola {Queue}; se descarta", QueueName);
            await SafeNackAsync(channel, args.DeliveryTag, stoppingToken);
            return;
        }

        var disposition = await MessageRetryExecutor.ExecuteAsync(
            handler: async ct =>
            {
                using var scope = _scopeFactory.CreateScope();
                await HandleMessageAsync(messageJson, scope.ServiceProvider, ct);
            },
            maxRetryAttempts: Settings.MaxRetryAttempts,
            baseDelay: TimeSpan.FromSeconds(Settings.RetryDelaySeconds),
            logger: _logger,
            context: QueueName,
            cancellationToken: stoppingToken);

        try
        {
            if (disposition == MessageDisposition.Acknowledged)
            {
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
            }
            else
            {
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        }
        catch (Exception exception)
        {
            // Never let ack/nack failures bubble up and tear down the consumer callback.
            _logger.LogError(
                exception,
                "Error al confirmar el mensaje en la cola {Queue}",
                QueueName);
        }
    }

    private async Task SafeNackAsync(IChannel channel, ulong deliveryTag, CancellationToken ct)
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, ct);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error al rechazar el mensaje en la cola {Queue}", QueueName);
        }
    }

    private async Task SafeCloseChannelAsync()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            await _channel.DisposeAsync();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Error al cerrar el canal de la cola {Queue}", QueueName);
        }
        finally
        {
            _channel = null;
        }
    }
}
