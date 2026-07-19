using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Commands.AssignTicket;

public record AssignTicketCommand(Guid TicketId, Guid AssignedToUserId) : IRequest<TicketDto>;
