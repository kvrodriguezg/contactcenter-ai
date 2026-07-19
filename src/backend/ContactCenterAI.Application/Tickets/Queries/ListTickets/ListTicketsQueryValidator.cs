using ContactCenterAI.Domain.Tickets;
using FluentValidation;

namespace ContactCenterAI.Application.Tickets.Queries.ListTickets;

public class ListTicketsQueryValidator : AbstractValidator<ListTicketsQuery>
{
    public ListTicketsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.TryParse<TicketStatus>(status, ignoreCase: true, out _))
            .WithMessage("El estado debe ser Pending, InReview, Resolved o Closed.");

        RuleFor(x => x.Priority)
            .Must(priority => priority is null || Enum.TryParse<TicketPriority>(priority, ignoreCase: true, out _))
            .WithMessage("La prioridad debe ser Low, Medium, High o Critical.");
    }
}
