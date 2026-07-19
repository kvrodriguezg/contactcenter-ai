using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Tickets;
using Microsoft.Extensions.Logging.Abstractions;
using DomainTicketCreatedEvent = ContactCenterAI.Application.Tickets.Events.TicketCreatedEvent;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class TicketEventPublisherTests
{
    private sealed class RecordingEventPublisher : IEventPublisher
    {
        public List<object> Published { get; } = [];

        public Exception? ThrowOnPublish { get; set; }

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class
        {
            if (ThrowOnPublish is not null)
            {
                throw ThrowOnPublish;
            }

            Published.Add(@event!);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Publishes_messaging_ticket_created_event()
    {
        var recorder = new RecordingEventPublisher();
        var publisher = new TicketEventPublisher(recorder, NullLogger<TicketEventPublisher>.Instance);
        var correlationId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        await publisher.PublishTicketCreatedAsync(
            new DomainTicketCreatedEvent(
                ticketId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Asunto",
                "High",
                DateTime.UtcNow,
                correlationId));

        var published = Assert.IsType<TicketCreatedEvent>(Assert.Single(recorder.Published));
        Assert.Equal(ticketId, published.TicketId);
        Assert.Equal("High", published.Priority);
        Assert.Equal(correlationId, published.CorrelationId);
    }

    [Fact]
    public async Task Controlled_error_does_not_throw_when_broker_fails()
    {
        var recorder = new RecordingEventPublisher
        {
            ThrowOnPublish = new InvalidOperationException("broker down")
        };
        var publisher = new TicketEventPublisher(recorder, NullLogger<TicketEventPublisher>.Instance);

        await publisher.PublishTicketCreatedAsync(
            new DomainTicketCreatedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Asunto",
                "Low",
                DateTime.UtcNow,
                Guid.NewGuid()));
    }
}
