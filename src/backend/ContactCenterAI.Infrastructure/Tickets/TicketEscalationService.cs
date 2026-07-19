using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Infrastructure.Tickets;

/// <summary>
/// Real, observable escalation stage: records processing timestamp/status and prepares the
/// ticket for a subsequent assignment flow. Does not create duplicate tickets.
/// </summary>
public sealed class TicketEscalationService : ITicketEscalationService
{
    public const string PreparedForAssignmentStatus = "PreparedForAssignment";

    private readonly IApplicationDbContext _context;
    private readonly ILogger<TicketEscalationService> _logger;

    public TicketEscalationService(
        IApplicationDbContext context,
        ILogger<TicketEscalationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TicketEscalationOutcome> ProcessEscalationAsync(
        Guid ticketId,
        string priority,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            _logger.LogWarning(
                "Escalación omitida: ticket {TicketId} no encontrado (CorrelationId={CorrelationId})",
                ticketId,
                correlationId);
            return TicketEscalationOutcome.NotFound;
        }

        if (ticket.EscalationProcessedAt is not null)
        {
            _logger.LogInformation(
                "Escalación idempotente omitida para ticket {TicketId}: ya procesado en {ProcessedAt:o} "
                + "(Status={EscalationStatus}, CorrelationId={CorrelationId})",
                ticket.Id,
                ticket.EscalationProcessedAt,
                ticket.EscalationStatus,
                correlationId);
            return TicketEscalationOutcome.SkippedAlreadyProcessed;
        }

        var processedAt = DateTime.UtcNow;
        ticket.EscalationProcessedAt = processedAt;
        ticket.EscalationStatus = PreparedForAssignmentStatus;
        ticket.UpdatedAt = processedAt;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Escalación preparada para ticket {TicketId}: CompanyId={CompanyId}, Priority={Priority}, "
            + "EscalationStatus={EscalationStatus}, EscalationProcessedAt={ProcessedAt:o}, "
            + "CorrelationId={CorrelationId}. Pendiente de asignación.",
            ticket.Id,
            ticket.CompanyId,
            priority,
            ticket.EscalationStatus,
            processedAt,
            correlationId);

        return TicketEscalationOutcome.Prepared;
    }
}
