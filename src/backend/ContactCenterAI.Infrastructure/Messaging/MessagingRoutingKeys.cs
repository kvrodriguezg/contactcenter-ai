using ContactCenterAI.Application.Common.Messaging;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>
/// Maps integration event types to topic-exchange routing keys. Shared by the publisher
/// (Infrastructure, used by the API) and the consumers (Worker) so both agree on the topology.
/// </summary>
public static class MessagingRoutingKeys
{
    public const string DocumentUploaded = "document.uploaded";

    public const string TicketCreated = "ticket.created";

    private static readonly IReadOnlyDictionary<Type, string> Map = new Dictionary<Type, string>
    {
        [typeof(DocumentUploadedEvent)] = DocumentUploaded,
        [typeof(TicketCreatedEvent)] = TicketCreated
    };

    /// <summary>Returns the routing key for an event type, or null if the type is not mapped.</summary>
    public static string? Resolve(Type eventType) =>
        Map.TryGetValue(eventType, out var routingKey) ? routingKey : null;
}
