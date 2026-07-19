using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Queries.GetTicketById;

public record GetTicketByIdQuery(Guid Id) : IRequest<TicketDto>;
