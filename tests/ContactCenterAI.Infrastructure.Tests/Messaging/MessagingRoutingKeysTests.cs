using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Messaging;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class MessagingRoutingKeysTests
{
    [Fact]
    public void Resolves_document_and_ticket_routing_keys()
    {
        Assert.Equal(
            MessagingRoutingKeys.DocumentUploaded,
            MessagingRoutingKeys.Resolve(typeof(DocumentUploadedEvent)));
        Assert.Equal(
            MessagingRoutingKeys.TicketCreated,
            MessagingRoutingKeys.Resolve(typeof(TicketCreatedEvent)));
    }

    [Fact]
    public void Unknown_event_type_returns_null()
    {
        Assert.Null(MessagingRoutingKeys.Resolve(typeof(string)));
    }
}
