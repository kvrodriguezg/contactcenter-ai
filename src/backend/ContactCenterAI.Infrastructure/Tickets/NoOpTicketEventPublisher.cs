using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Events;

namespace ContactCenterAI.Infrastructure.Tickets;

/// <summary>
/// No-op publisher placeholder until RabbitMQ (or another broker) is integrated.
/// </summary>
public sealed class NoOpTicketEventPublisher : ITicketEventPublisher
{
    public Task PublishTicketCreatedAsync(
        TicketCreatedEvent @event,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
