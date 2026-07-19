using FluentValidation;

namespace ContactCenterAI.Application.Chat.Commands.AskQuestion;

public class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(command => command.Question)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(command => command.TopK)
            .InclusiveBetween(1, 20);
    }
}
