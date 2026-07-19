using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.DTOs;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var actorRole = _currentUserService.Role;
        if (actorRole is not (Role.SuperAdmin or Role.CompanyAdmin))
        {
            throw new UnauthorizedAccessException("No tiene permisos para editar usuarios.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario especificado no existe.");

        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var companyId = request.CompanyId;

        // CompanyAdmin is scoped to its own company and cannot manage SuperAdmins.
        if (actorRole == Role.CompanyAdmin)
        {
            if (_currentUserService.CompanyId is null)
            {
                throw new UnauthorizedAccessException("El administrador de empresa debe pertenecer a una empresa.");
            }

            if (user.CompanyId != _currentUserService.CompanyId)
            {
                throw new UnauthorizedAccessException("Solo puede editar usuarios de su propia empresa.");
            }

            if (role == Role.SuperAdmin || user.Role == Role.SuperAdmin)
            {
                throw new UnauthorizedAccessException("No tiene permisos para gestionar usuarios SuperAdmin.");
            }

            companyId ??= _currentUserService.CompanyId;

            if (companyId != _currentUserService.CompanyId)
            {
                throw new UnauthorizedAccessException("No puede mover usuarios a otra empresa.");
            }
        }

        // Non-SuperAdmin accounts must belong to a company.
        if (role != Role.SuperAdmin && companyId is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(UpdateUserCommand.CompanyId),
                    "Debe asignar una empresa para este rol.")
            ]);
        }

        Company? company = null;
        if (companyId is not null)
        {
            company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
                ?? throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(UpdateUserCommand.CompanyId),
                        "La empresa especificada no existe.")
                ]);

            // Only enforce the "active company" rule when the company assignment changes,
            // so existing users linked to a deactivated company can still be edited.
            if (company.Status != CompanyStatus.Active && companyId != user.CompanyId)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(UpdateUserCommand.CompanyId),
                        "La empresa especificada no está activa.")
                ]);
            }
        }

        user.Role = role;
        user.IsActive = request.IsActive;
        user.CompanyId = companyId;
        if (request.Name is not null)
        {
            user.Name = request.Name.Trim().Length == 0 ? null : request.Name.Trim();
        }
        user.UpdatedAt = DateTime.UtcNow;
        // ExternalSubject / AuthenticationProvider are intentionally left untouched
        // to preserve compatibility with Auth0-linked users.

        await _context.SaveChangesAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CompanyId = user.CompanyId,
            CompanyName = company?.Name,
            AuthenticationProvider = user.AuthenticationProvider.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}
