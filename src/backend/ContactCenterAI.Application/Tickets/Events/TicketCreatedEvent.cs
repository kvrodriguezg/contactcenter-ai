namespace ContactCenterAI.Application.Tickets.Events;

/// <summary>
/// Domain event raised when a ticket is created. Mapped to the messaging integration contract
/// by <c>ITicketEventPublisher</c> without coupling the use-case to the broker.
/// </summary>
public sealed record TicketCreatedEvent(
    Guid TicketId,
    Guid CompanyId,
    Guid CreatedByUserId,
    string Subject,
    string Priority,
    DateTime OccurredAt,
    Guid CorrelationId);
