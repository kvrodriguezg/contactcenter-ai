using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Domain.Tickets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Commands.ResolveTicket;

public class ResolveTicketCommandHandler : IRequestHandler<ResolveTicketCommand, TicketDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ResolveTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketDto> Handle(ResolveTicketCommand request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureCanManage(_currentUserService);

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket no encontrado.");

        TicketAuthorization.EnsureTicketCompanyAccess(ticket, _currentUserService);

        var now = DateTime.UtcNow;
        ticket.Resolution = request.Resolution.Trim();
        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAt = now;
        ticket.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking())
            .FirstAsync(t => t.Id == ticket.Id, cancellationToken);

        return TicketAuthorization.ToDto(updated);
    }
}
