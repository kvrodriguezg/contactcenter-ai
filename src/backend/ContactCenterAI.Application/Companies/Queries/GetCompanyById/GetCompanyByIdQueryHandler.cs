using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Companies.Queries.GetCompanyById;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanyByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        // Non-SuperAdmin users may only read their own company.
        if (_currentUserService.Role != Role.SuperAdmin &&
            _currentUserService.CompanyId != request.Id)
        {
            throw new UnauthorizedAccessException("No tiene permisos para consultar esta empresa.");
        }

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La empresa especificada no existe.");

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Status = company.Status.ToString(),
            CreatedAt = company.CreatedAt
        };
    }
}
