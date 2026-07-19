using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using FluentValidation;

namespace ContactCenterAI.Application.Documents.Commands.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public UploadDocumentCommandValidator(ICurrentUserService currentUserService)
    {
        RuleFor(x => x)
            .Must(_ => currentUserService.IsAuthenticated)
            .WithMessage("El usuario debe estar autenticado.");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("El archivo es obligatorio.");

        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .WithMessage("El archivo es obligatorio.");

        RuleFor(x => x.ContentType)
            .Equal("application/pdf")
            .WithMessage("Solo se permiten archivos PDF.");

        RuleFor(x => x.OriginalFileName)
            .Must(fileName => fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Solo se permiten archivos PDF.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithMessage("El archivo es obligatorio.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("El archivo no puede superar los 10 MB.");

        When(_ => currentUserService.Role != Role.SuperAdmin, () =>
        {
            RuleFor(x => x)
                .Must(_ => currentUserService.CompanyId.HasValue)
                .WithMessage("El usuario debe pertenecer a una empresa.");
        });
    }
}
