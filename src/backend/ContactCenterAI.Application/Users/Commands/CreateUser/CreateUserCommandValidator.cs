using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.Common;
using ContactCenterAI.Domain.Identity;
using FluentValidation;

namespace ContactCenterAI.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IAuthProviderMode authProviderMode)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress()
            .WithMessage("El correo electrónico no es válido.")
            .MaximumLength(256)
            .WithMessage("El correo electrónico no puede superar los 256 caracteres.");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Role)
            .Must(role => Enum.TryParse<Role>(role, ignoreCase: true, out _))
            .WithMessage("El rol especificado no es válido.");

        RuleFor(x => x.Password)
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.ExternalSubject)
            .Must(value => ExternalSubjectRules.Normalize(value) is not null)
            .WithMessage(ExternalSubjectRules.RequiredMessage)
            .When(_ => authProviderMode.IsAuth0);

        RuleFor(x => x.ExternalSubject)
            .Must(value =>
            {
                var normalized = ExternalSubjectRules.Normalize(value);
                return normalized is null || normalized.Length <= ExternalSubjectRules.MaxLength;
            })
            .WithMessage(ExternalSubjectRules.MaxLengthMessage)
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalSubject));
    }
}
