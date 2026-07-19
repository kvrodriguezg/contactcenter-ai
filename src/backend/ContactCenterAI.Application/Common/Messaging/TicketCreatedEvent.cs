namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Published when a ticket is created (by the tickets feature, owned by another subagent).
/// Defined here as a shared integration contract so this worktree compiles independently.
/// Matches architecture report §15.4.
/// </summary>
public record TicketCreatedEvent(
    Guid TicketId,
    Guid CompanyId,
    Guid CreatedByUserId,
    string Subject,
    DateTime OccurredAtUtc);
