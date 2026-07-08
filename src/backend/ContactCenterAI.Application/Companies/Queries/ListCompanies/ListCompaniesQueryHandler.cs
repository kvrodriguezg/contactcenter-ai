using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Companies.Queries.ListCompanies;

public class ListCompaniesQueryHandler : IRequestHandler<ListCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListCompaniesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CompanyDto>> Handle(ListCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Companies.AsNoTracking();

        if (_currentUserService.Role != Role.SuperAdmin)
        {
            if (_currentUserService.CompanyId is null)
            {
                return [];
            }

            query = query.Where(c => c.Id == _currentUserService.CompanyId);
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
