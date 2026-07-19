namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>
/// Maps Core <c>TicketDto</c>. Status/Priority arrive as strings from REST and are
/// exposed as GraphQL enums via the type layer.
/// </summary>
public class Ticket
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
