using FluentValidation;

namespace ContactCenterAI.Application.Tickets.Commands.AssignTicket;

public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
