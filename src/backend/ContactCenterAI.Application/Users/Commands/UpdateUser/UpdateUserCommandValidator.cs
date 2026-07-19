using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.Common;
using ContactCenterAI.Domain.Identity;
using FluentValidation;

namespace ContactCenterAI.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IAuthProviderMode authProviderMode)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.Role)
            .Must(role => Enum.TryParse<Role>(role, ignoreCase: true, out _))
            .WithMessage("El rol especificado no es válido.");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.ExternalSubject)
            .Must(value => ExternalSubjectRules.Normalize(value) is not null)
            .WithMessage(ExternalSubjectRules.RequiredMessage)
            .When(x => authProviderMode.IsAuth0 && x.ExternalSubject is not null);

        RuleFor(x => x.ExternalSubject)
            .Must(value =>
            {
                var normalized = ExternalSubjectRules.Normalize(value);
                return normalized is null || normalized.Length <= ExternalSubjectRules.MaxLength;
            })
            .WithMessage(ExternalSubjectRules.MaxLengthMessage)
            .When(x => x.ExternalSubject is not null && !string.IsNullOrWhiteSpace(x.ExternalSubject));
    }
}
