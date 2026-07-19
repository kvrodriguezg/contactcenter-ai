namespace ContactCenterAI.Application.Tickets.DTOs;

public class TicketDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public string CreatedByEmail { get; set; } = string.Empty;

    public string? CreatedByName { get; set; }

    public Guid? ConversationId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public Guid? AssignedToUserId { get; set; }

    public string? AssignedToEmail { get; set; }

    public string? AssignedToName { get; set; }

    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? EscalationProcessedAt { get; set; }

    public string? EscalationStatus { get; set; }
}
