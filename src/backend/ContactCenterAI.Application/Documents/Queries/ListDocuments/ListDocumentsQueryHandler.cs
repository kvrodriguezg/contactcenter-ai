using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Documents.Queries.ListDocuments;

public class ListDocumentsQueryHandler : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListDocumentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Documents.AsNoTracking();

        if (_currentUserService.Role == Role.SuperAdmin)
        {
            // SuperAdmin: sin restricción por empresa.
        }
        else if (_currentUserService.CompanyId is not null)
        {
            query = query.Where(d => d.CompanyId == _currentUserService.CompanyId);
        }
        else
        {
            return [];
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
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
            .ToListAsync(cancellationToken);
    }
}
