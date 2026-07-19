using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Queries.ListTickets;

public record ListTicketsQuery(
    string? Status = null,
    string? Priority = null) : IRequest<IReadOnlyList<TicketDto>>;
