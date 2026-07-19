namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Integration contract published after a ticket is successfully persisted.
/// Consumed by the Worker for the async escalation stage.
/// </summary>
public record TicketCreatedEvent(
    Guid TicketId,
    Guid CompanyId,
    Guid CreatedByUserId,
    string Priority,
    DateTime OccurredAt,
    Guid CorrelationId);
