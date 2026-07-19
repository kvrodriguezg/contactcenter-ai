using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Documents.Queries.SearchDocuments;

public class SearchDocumentsQueryHandler
    : IRequestHandler<SearchDocumentsQuery, IReadOnlyList<SemanticSearchResultDto>>
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SearchDocumentsQueryHandler(
        ISemanticSearchService semanticSearchService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _semanticSearchService = semanticSearchService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<SemanticSearchResultDto>> Handle(
        SearchDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("El usuario debe estar autenticado.");
        }

        var companyId = await ResolveCompanyIdAsync(request.CompanyId, cancellationToken);

        return await _semanticSearchService.SearchSimilarChunksAsync(
            companyId,
            request.Query,
            request.TopK,
            cancellationToken);
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
                        nameof(SearchDocumentsQuery.CompanyId),
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
            throw new UnauthorizedAccessException("No tiene permisos para buscar en otra empresa.");
        }

        return _currentUserService.CompanyId.Value;
    }
}
