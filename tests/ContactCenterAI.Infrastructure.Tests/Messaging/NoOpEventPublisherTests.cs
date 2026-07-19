using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class NoOpEventPublisherTests
{
    [Fact]
    public async Task Messaging_disabled_publisher_completes_without_error()
    {
        var publisher = new NoOpEventPublisher(NullLogger<NoOpEventPublisher>.Instance);

        await publisher.PublishAsync(
            new DocumentUploadedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid()));

        await publisher.PublishAsync(
            new TicketCreatedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Medium",
                DateTime.UtcNow,
                Guid.NewGuid()));
    }
}
