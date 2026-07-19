using ContactCenterAI.Application.Tickets.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Tickets.Commands.CreateTicket;

public record CreateTicketCommand(
    string Subject,
    string Description,
    string Priority,
    Guid? ConversationId = null,
    Guid? CompanyId = null) : IRequest<TicketDto>;
