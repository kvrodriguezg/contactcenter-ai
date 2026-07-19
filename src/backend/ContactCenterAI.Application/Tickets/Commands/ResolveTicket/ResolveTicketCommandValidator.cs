using FluentValidation;

namespace ContactCenterAI.Application.Tickets.Commands.ResolveTicket;

public class ResolveTicketCommandValidator : AbstractValidator<ResolveTicketCommand>
{
    public ResolveTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("La resolución es obligatoria.")
            .MaximumLength(4000);
    }
}
