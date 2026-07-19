namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>Outcome of the async ticket escalation stage.</summary>
public enum TicketEscalationOutcome
{
    /// <summary>Escalation markers were written; ticket is ready for assignment.</summary>
    Prepared = 0,

    /// <summary>Escalation already ran; duplicate delivery skipped.</summary>
    SkippedAlreadyProcessed = 1,

    /// <summary>Ticket id from the event does not exist.</summary>
    NotFound = 2
}

/// <summary>
/// Performs the observable async escalation stage after <c>TicketCreatedEvent</c>.
/// Idempotent: a ticket with <c>EscalationProcessedAt</c> set is never mutated again.
/// </summary>
public interface ITicketEscalationService
{
    Task<TicketEscalationOutcome> ProcessEscalationAsync(
        Guid ticketId,
        string priority,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
