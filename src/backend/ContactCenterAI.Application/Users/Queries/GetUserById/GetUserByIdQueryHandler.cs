using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario especificado no existe.");

        // Non-SuperAdmin users may only read users within their own company.
        if (_currentUserService.Role != Role.SuperAdmin &&
            (_currentUserService.CompanyId is null || user.CompanyId != _currentUserService.CompanyId))
        {
            throw new UnauthorizedAccessException("No tiene permisos para consultar este usuario.");
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            AuthenticationProvider = user.AuthenticationProvider.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}
