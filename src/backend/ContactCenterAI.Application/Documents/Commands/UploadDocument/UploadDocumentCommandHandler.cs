using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Documents.Commands.UploadDocument;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentStorageService _documentStorageService;

    public UploadDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentStorageService documentStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentStorageService = documentStorageService;
    }

    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("El usuario debe estar autenticado.");
        }

        var companyId = await ResolveCompanyIdAsync(request.CompanyId, cancellationToken);

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("La empresa especificada no existe.");

        var documentId = Guid.NewGuid();
        var storedFileName = $"{documentId}.pdf";

        var storagePath = await _documentStorageService.SaveAsync(
            companyId,
            documentId,
            storedFileName,
            request.FileStream,
            cancellationToken);

        var document = new Document
        {
            Id = documentId,
            CompanyId = companyId,
            UploadedByUserId = _currentUserService.UserId.Value,
            FileName = storedFileName,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = storagePath,
            Status = DocumentStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        return new DocumentDto
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            SizeBytes = document.SizeBytes,
            Status = document.Status.ToString(),
            CompanyId = document.CompanyId,
            CompanyName = company.Name,
            UploadedByUserId = document.UploadedByUserId,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt,
            ErrorMessage = document.ErrorMessage
        };
    }

    private async Task<Guid> ResolveCompanyIdAsync(Guid? requestedCompanyId, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role == Role.SuperAdmin)
        {
            if (requestedCompanyId.HasValue)
            {
                return requestedCompanyId.Value;
            }

            var defaultCompanyId = await _context.Companies
                .AsNoTracking()
                .OrderBy(c => c.CreatedAt)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultCompanyId == Guid.Empty)
            {
                throw new ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(UploadDocumentCommand.CompanyId),
                        "Debe especificar una empresa o existir al menos una empresa en el sistema.")
                ]);
            }

            return defaultCompanyId;
        }

        if (_currentUserService.CompanyId is null)
        {
            throw new UnauthorizedAccessException("El usuario debe pertenecer a una empresa.");
        }

        if (requestedCompanyId.HasValue && requestedCompanyId != _currentUserService.CompanyId)
        {
            throw new UnauthorizedAccessException("No tiene permisos para cargar documentos en otra empresa.");
        }

        return _currentUserService.CompanyId.Value;
    }
}
