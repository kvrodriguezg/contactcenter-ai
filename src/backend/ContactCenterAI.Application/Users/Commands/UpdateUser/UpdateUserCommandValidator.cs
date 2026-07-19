using ContactCenterAI.Domain.Identity;
using FluentValidation;

namespace ContactCenterAI.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
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
    }
}
