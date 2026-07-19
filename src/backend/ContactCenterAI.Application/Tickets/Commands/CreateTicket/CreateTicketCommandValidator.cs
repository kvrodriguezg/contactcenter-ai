using ContactCenterAI.Domain.Tickets;
using FluentValidation;

namespace ContactCenterAI.Application.Tickets.Commands.CreateTicket;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("El asunto es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(4000);

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(priority => Enum.TryParse<TicketPriority>(priority, ignoreCase: true, out _))
            .WithMessage("La prioridad debe ser Low, Medium, High o Critical.");
    }
}
