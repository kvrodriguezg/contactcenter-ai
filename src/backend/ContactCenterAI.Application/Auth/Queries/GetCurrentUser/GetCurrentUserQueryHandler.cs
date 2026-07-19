using ContactCenterAI.Application.Auth.DTOs;
using ContactCenterAI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException(
                _currentUserService.AuthorizationFailureMessage
                ?? "Usuario no autenticado.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Usuario no encontrado.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("El usuario está inactivo.");
        }

        return new CurrentUserDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            IsActive = user.IsActive
        };
    }
}
