using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Companies.Commands.SetCompanyStatus;

public class SetCompanyStatusCommandHandler : IRequestHandler<SetCompanyStatusCommand, CompanyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetCompanyStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto> Handle(SetCompanyStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != Role.SuperAdmin)
        {
            throw new UnauthorizedAccessException("Solo un SuperAdmin puede cambiar el estado de una empresa.");
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La empresa especificada no existe.");

        company.Status = request.Status;
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
