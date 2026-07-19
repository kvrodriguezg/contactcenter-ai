using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Application.Documents.Commands.UploadDocument;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IEventPublisher _eventPublisher;
    private readonly MessagingSettings _messagingSettings;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;

    public UploadDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentStorageService documentStorageService,
        IEventPublisher eventPublisher,
        IOptions<MessagingSettings> messagingSettings,
        ILogger<UploadDocumentCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentStorageService = documentStorageService;
        _eventPublisher = eventPublisher;
        _messagingSettings = messagingSettings.Value;
        _logger = logger;
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

        // When messaging is enabled the document is "enqueued" for event-driven processing, so it
        // starts in PendingProcessing. With messaging disabled it stays Uploaded (unchanged behavior).
        // Either state is picked up by the polling reconciliation loop, so nothing is ever lost.
        var initialStatus = _messagingSettings.Enabled
            ? DocumentStatus.PendingProcessing
            : DocumentStatus.Uploaded;

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
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        await PublishDocumentUploadedAsync(document, cancellationToken);

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

    private async Task PublishDocumentUploadedAsync(Document document, CancellationToken cancellationToken)
    {
        // Publish AFTER a successful save. A broker outage must never fail the upload — the polling
        // reconciliation loop will still pick the document up — so failures are logged, not thrown.
        try
        {
            await _eventPublisher.PublishAsync(
                new DocumentUploadedEvent(
                    document.Id,
                    document.CompanyId,
                    document.UploadedByUserId,
                    DateTime.UtcNow,
                    Guid.NewGuid()),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo publicar DocumentUploadedEvent para {DocumentId}; el polling lo procesará",
                document.Id);
        }
    }
}
