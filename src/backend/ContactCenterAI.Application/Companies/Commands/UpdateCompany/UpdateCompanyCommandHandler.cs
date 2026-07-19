using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != Role.SuperAdmin)
        {
            throw new UnauthorizedAccessException("Solo un SuperAdmin puede editar empresas.");
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La empresa especificada no existe.");

        var name = request.Name.Trim();
        var normalized = name.ToLower();

        var nameExists = await _context.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id != request.Id && c.Name.ToLower() == normalized, cancellationToken);

        if (nameExists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(UpdateCompanyCommand.Name),
                    "Ya existe una empresa con ese nombre.")
            ]);
        }

        company.Name = name;
        company.Status = Enum.Parse<CompanyStatus>(request.Status, ignoreCase: true);
        company.UpdatedAt = DateTime.UtcNow;

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
