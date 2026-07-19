using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;

namespace ContactCenterAI.Domain.Tickets;

public class Ticket : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public Guid? ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    public Guid? AssignedToUserId { get; set; }

    public User? AssignedToUser { get; set; }

    public string? Resolution { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// UTC timestamp of the async escalation stage (Worker). Null until processed; set once for idempotency.
    /// </summary>
    public DateTime? EscalationProcessedAt { get; set; }

    /// <summary>
    /// Observable escalation outcome (e.g. PreparedForAssignment). Null until the Worker consumes the event.
    /// </summary>
    public string? EscalationStatus { get; set; }
}
