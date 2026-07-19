namespace ContactCenterAI.Application.Tickets.Events;

/// <summary>
/// Domain event raised when a ticket is created.
/// Extension point for future messaging (e.g. RabbitMQ); no broker integration yet.
/// </summary>
public sealed record TicketCreatedEvent(
    Guid TicketId,
    Guid CompanyId,
    Guid CreatedByUserId,
    string Subject,
    string Priority,
    DateTime OccurredAtUtc);
