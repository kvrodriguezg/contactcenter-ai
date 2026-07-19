using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateCompanyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != Role.SuperAdmin)
        {
            throw new UnauthorizedAccessException("Solo un SuperAdmin puede crear empresas.");
        }

        var name = request.Name.Trim();
        var normalized = name.ToLower();

        var nameExists = await _context.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Name.ToLower() == normalized, cancellationToken);

        if (nameExists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateCompanyCommand.Name),
                    "Ya existe una empresa con ese nombre.")
            ]);
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Status = company.Status.ToString(),
            CreatedAt = company.CreatedAt
        };
    }
}
