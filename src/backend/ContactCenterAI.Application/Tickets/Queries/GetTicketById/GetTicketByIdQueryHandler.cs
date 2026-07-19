using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTicketByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureAuthenticated(_currentUserService);

        var query = TicketAuthorization.ApplyCompanyScope(
            TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking()),
            _currentUserService);

        var ticket = await query.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException("Ticket no encontrado.");
        }

        return TicketAuthorization.ToDto(ticket);
    }
}
