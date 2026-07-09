using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Documents.Queries.GetDocumentById;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DocumentDto> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Documents.AsNoTracking();

        if (_currentUserService.Role == Role.SuperAdmin)
        {
            // Sin filtro adicional.
        }
        else if (_currentUserService.CompanyId is not null)
        {
            query = query.Where(d => d.CompanyId == _currentUserService.CompanyId);
        }
        else
        {
            throw new KeyNotFoundException("Documento no encontrado.");
        }

        var document = await query
            .Where(d => d.Id == request.Id)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                OriginalFileName = d.OriginalFileName,
                SizeBytes = d.SizeBytes,
                Status = d.Status.ToString(),
                CompanyId = d.CompanyId,
                CompanyName = d.Company.Name,
                UploadedByUserId = d.UploadedByUserId,
                CreatedAt = d.CreatedAt,
                ProcessedAt = d.ProcessedAt,
                ErrorMessage = d.ErrorMessage
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("Documento no encontrado.");
        }

        return document;
    }
}
