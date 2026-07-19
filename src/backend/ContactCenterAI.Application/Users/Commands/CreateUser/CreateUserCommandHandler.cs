using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.Common;
using ContactCenterAI.Application.Users.DTOs;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthProviderMode _authProviderMode;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IAuthProviderMode authProviderMode)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _authProviderMode = authProviderMode;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var actorRole = _currentUserService.Role;
        if (actorRole is not (Role.SuperAdmin or Role.CompanyAdmin))
        {
            throw new UnauthorizedAccessException("No tiene permisos para crear usuarios.");
        }

        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var companyId = request.CompanyId;

        // CompanyAdmin is scoped to its own company and cannot create SuperAdmins.
        if (actorRole == Role.CompanyAdmin)
        {
            if (_currentUserService.CompanyId is null)
            {
                throw new UnauthorizedAccessException("El administrador de empresa debe pertenecer a una empresa.");
            }

            if (role == Role.SuperAdmin)
            {
                throw new UnauthorizedAccessException("No tiene permisos para crear usuarios SuperAdmin.");
            }

            companyId ??= _currentUserService.CompanyId;

            if (companyId != _currentUserService.CompanyId)
            {
                throw new UnauthorizedAccessException("Solo puede crear usuarios en su propia empresa.");
            }
        }

        // Non-SuperAdmin accounts must belong to a company.
        if (role != Role.SuperAdmin && companyId is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateUserCommand.CompanyId),
                    "Debe asignar una empresa para este rol.")
            ]);
        }

        var email = request.Email.Trim();
        var normalizedEmail = email.ToLower();

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(CreateUserCommand.Email),
                    "Ya existe un usuario con ese correo electrónico.")
            ]);
        }

        var externalSubject = ExternalSubjectRules.Normalize(request.ExternalSubject);
        ExternalSubjectRules.EnsureValidForCreate(
            _authProviderMode,
            externalSubject,
            nameof(CreateUserCommand.ExternalSubject));

        await ExternalSubjectRules.EnsureUniqueAsync(
            _context,
            externalSubject,
            excludeUserId: null,
            nameof(CreateUserCommand.ExternalSubject),
            cancellationToken);

        Company? company = null;
        if (companyId is not null)
        {
            company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
                ?? throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(CreateUserCommand.CompanyId),
                        "La empresa especificada no existe.")
                ]);

            if (company.Status != CompanyStatus.Active)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(CreateUserCommand.CompanyId),
                        "La empresa especificada no está activa.")
                ]);
            }
        }

        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            Role = role,
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        ExternalSubjectRules.ApplyToUser(user, externalSubject);

        user.PasswordHash = string.IsNullOrEmpty(request.Password)
            ? string.Empty
            : _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(user, company?.Name);
    }

    private static UserDto ToDto(User user, string? companyName) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CompanyId = user.CompanyId,
        CompanyName = companyName,
        AuthenticationProvider = user.AuthenticationProvider.ToString(),
        ExternalSubject = user.ExternalSubject,
        CreatedAt = user.CreatedAt
    };
}
