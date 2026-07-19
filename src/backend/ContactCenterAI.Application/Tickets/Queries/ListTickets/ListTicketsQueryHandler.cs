using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Domain.Tickets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Queries.ListTickets;

public class ListTicketsQueryHandler : IRequestHandler<ListTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListTicketsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<TicketDto>> Handle(ListTicketsQuery request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureAuthenticated(_currentUserService);

        var query = TicketAuthorization.ApplyCompanyScope(
            TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking()),
            _currentUserService);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority) &&
            Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tickets.Select(TicketAuthorization.ToDto).ToList();
    }
}
