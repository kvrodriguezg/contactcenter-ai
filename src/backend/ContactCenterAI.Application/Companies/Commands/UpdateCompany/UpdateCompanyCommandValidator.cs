using ContactCenterAI.Domain.Tenancy;
using FluentValidation;

namespace ContactCenterAI.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador de la empresa es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre de la empresa es obligatorio.")
            .MaximumLength(200)
            .WithMessage("El nombre de la empresa no puede superar los 200 caracteres.");

        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<CompanyStatus>(status, ignoreCase: true, out _))
            .WithMessage("El estado de la empresa no es válido.");
    }
}
