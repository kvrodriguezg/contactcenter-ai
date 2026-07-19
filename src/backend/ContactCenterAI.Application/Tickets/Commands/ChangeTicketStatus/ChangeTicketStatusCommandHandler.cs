using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Domain.Tickets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Commands.ChangeTicketStatus;

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand, TicketDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChangeTicketStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketDto> Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureCanManage(_currentUserService);

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket no encontrado.");

        TicketAuthorization.EnsureTicketCompanyAccess(ticket, _currentUserService);

        var status = Enum.Parse<TicketStatus>(request.Status, ignoreCase: true);
        var now = DateTime.UtcNow;

        ticket.Status = status;
        ticket.UpdatedAt = now;

        if (status is TicketStatus.Resolved or TicketStatus.Closed && ticket.ResolvedAt is null)
        {
            ticket.ResolvedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking())
            .FirstAsync(t => t.Id == ticket.Id, cancellationToken);

        return TicketAuthorization.ToDto(updated);
    }
}
