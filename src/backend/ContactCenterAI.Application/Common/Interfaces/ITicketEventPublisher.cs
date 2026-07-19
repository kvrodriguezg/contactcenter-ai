using ContactCenterAI.Application.Tickets.Events;

namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>
/// Publishes ticket domain events via <see cref="IEventPublisher"/> (RabbitMQ or no-op).
/// </summary>
public interface ITicketEventPublisher
{
    Task PublishTicketCreatedAsync(TicketCreatedEvent @event, CancellationToken cancellationToken = default);
}
