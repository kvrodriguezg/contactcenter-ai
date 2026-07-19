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
}
