using ContactCenterAI.Domain.Tickets;
using FluentValidation;

namespace ContactCenterAI.Application.Tickets.Commands.ChangeTicketStatus;

public class ChangeTicketStatusCommandValidator : AbstractValidator<ChangeTicketStatusCommand>
{
    public ChangeTicketStatusCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => Enum.TryParse<TicketStatus>(status, ignoreCase: true, out _))
            .WithMessage("El estado debe ser Pending, InReview, Resolved o Closed.");
    }
}
