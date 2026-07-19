using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// Default publisher used when <c>Messaging:Enabled=false</c>. Does nothing but log at debug
/// level, so the system behaves exactly as before (pure DB polling) with zero RabbitMQ dependency.
/// </summary>
public sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class
    {
        _logger.LogDebug(
            "Mensajería deshabilitada; se omite la publicación del evento {EventType}",
            typeof(TEvent).Name);
        return Task.CompletedTask;
    }
}
