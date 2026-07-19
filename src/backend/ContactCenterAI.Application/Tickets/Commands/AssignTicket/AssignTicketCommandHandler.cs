using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Commands.AssignTicket;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, TicketDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AssignTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketDto> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureCanManage(_currentUserService);

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket no encontrado.");

        TicketAuthorization.EnsureTicketCompanyAccess(ticket, _currentUserService);

        var assignee = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.AssignedToUserId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario asignado no existe.");

        if (!assignee.IsActive)
        {
            throw new FluentValidation.ValidationException("El usuario asignado no está activo.");
        }

        if (assignee.CompanyId != ticket.CompanyId)
        {
            throw new UnauthorizedAccessException(
                "No puede asignar un usuario de otra empresa al ticket.");
        }

        if (_currentUserService.Role == Role.CompanyAdmin &&
            (_currentUserService.CompanyId is null ||
             assignee.CompanyId != _currentUserService.CompanyId ||
             ticket.CompanyId != _currentUserService.CompanyId))
        {
            throw new UnauthorizedAccessException(
                "No puede asignar usuarios fuera de su empresa.");
        }

        ticket.AssignedToUserId = assignee.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking())
            .FirstAsync(t => t.Id == ticket.Id, cancellationToken);

        return TicketAuthorization.ToDto(updated);
    }
}
