using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Application.Tickets.Events;
using Microsoft.Extensions.Logging;
using MessagingTicketCreatedEvent = ContactCenterAI.Application.Common.Messaging.TicketCreatedEvent;
using DomainTicketCreatedEvent = ContactCenterAI.Application.Tickets.Events.TicketCreatedEvent;

namespace ContactCenterAI.Infrastructure.Tickets;

/// <summary>
/// Bridges the ticket domain event to <see cref="IEventPublisher"/>. When messaging is disabled
/// the underlying publisher is a no-op; when enabled it publishes to RabbitMQ. Broker failures
/// are logged and swallowed so ticket creation never rolls back after a successful save.
/// </summary>
public sealed class TicketEventPublisher : ITicketEventPublisher
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TicketEventPublisher> _logger;

    public TicketEventPublisher(
        IEventPublisher eventPublisher,
        ILogger<TicketEventPublisher> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task PublishTicketCreatedAsync(
        DomainTicketCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _eventPublisher.PublishAsync(
                new MessagingTicketCreatedEvent(
                    @event.TicketId,
                    @event.CompanyId,
                    @event.CreatedByUserId,
                    @event.Priority,
                    @event.OccurredAt,
                    @event.CorrelationId),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo publicar TicketCreatedEvent para {TicketId} (CorrelationId={CorrelationId}); "
                + "el ticket permanece persistido",
                @event.TicketId,
                @event.CorrelationId);
        }
    }
}
