using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Events;

namespace ContactCenterAI.Application.Tests.Common;

public sealed class FakeTicketEventPublisher : ITicketEventPublisher
{
    public List<TicketCreatedEvent> Published { get; } = [];

    public Task PublishTicketCreatedAsync(
        TicketCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        Published.Add(@event);
        return Task.CompletedTask;
    }
}
