using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// Singleton owner of a single, lazily-created, auto-recovering RabbitMQ connection. The
/// underlying client performs automatic connection/topology recovery after transient network
/// drops; this wrapper additionally re-creates the connection if it was fully closed and
/// serializes concurrent creation attempts.
/// </summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;

    public RabbitMqConnection(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnection> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await SafeDisposeAsync(_connection);
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryIntervalSeconds)
            };

            _connection = await factory.CreateConnectionAsync(_settings.ClientProvidedName, cancellationToken);

            _logger.LogInformation(
                "Conexión RabbitMQ establecida con {Host}:{Port} (vhost {VirtualHost})",
                _settings.Host,
                _settings.Port,
                _settings.VirtualHost);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await SafeDisposeAsync(_connection);
            _connection = null;
        }

        _gate.Dispose();
    }

    private async Task SafeDisposeAsync(IConnection connection)
    {
        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Error al liberar la conexión RabbitMQ previa");
        }
    }
}
