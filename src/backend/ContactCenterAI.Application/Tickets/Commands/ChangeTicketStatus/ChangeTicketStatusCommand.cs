using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Commands.ChangeTicketStatus;

public record ChangeTicketStatusCommand(Guid TicketId, string Status) : IRequest<TicketDto>;
