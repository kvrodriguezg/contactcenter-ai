namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ connection + topology + resilience settings. Bound from the "RabbitMq" configuration
/// section (env vars <c>RabbitMq__Host</c>, <c>RabbitMq__Port</c>, ...). Defaults are safe for a
/// local docker-compose broker (guest/guest); never ship real credentials here.
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    // Connection
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string ClientProvidedName { get; set; } = "contactcenterai";

    // Topology
    public string ExchangeName { get; set; } = "contactcenter.events";

    public string DocumentProcessingQueue { get; set; } = "document.processing";

    public string TicketEscalationQueue { get; set; } = "ticket.escalation";

    // Resilience
    /// <summary>Interval used by the client's automatic recovery after a network drop.</summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>Delay before retrying the initial connection/subscription when the broker is down.</summary>
    public int ReconnectDelaySeconds { get; set; } = 5;

    /// <summary>Max in-process retries for a single message before it is discarded (no requeue).</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay between message retries (multiplied by the attempt number for backoff).</summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>Per-consumer unacked message prefetch limit.</summary>
    public ushort PrefetchCount { get; set; } = 10;
}
