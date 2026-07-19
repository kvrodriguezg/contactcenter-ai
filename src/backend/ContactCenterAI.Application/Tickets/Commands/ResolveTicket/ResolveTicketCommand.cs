using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Commands.ResolveTicket;

public record ResolveTicketCommand(Guid TicketId, string Resolution) : IRequest<TicketDto>;
