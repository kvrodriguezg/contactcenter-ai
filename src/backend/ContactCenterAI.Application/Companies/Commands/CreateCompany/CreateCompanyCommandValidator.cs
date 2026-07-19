using FluentValidation;

namespace ContactCenterAI.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre de la empresa es obligatorio.")
            .MaximumLength(200)
            .WithMessage("El nombre de la empresa no puede superar los 200 caracteres.");
    }
}
