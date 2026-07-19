using ContactCenterAI.Application.Tickets.Events;

namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>
/// Publishes ticket domain events. Default implementation is a no-op until messaging is wired.
/// </summary>
public interface ITicketEventPublisher
{
    Task PublishTicketCreatedAsync(TicketCreatedEvent @event, CancellationToken cancellationToken = default);
}
