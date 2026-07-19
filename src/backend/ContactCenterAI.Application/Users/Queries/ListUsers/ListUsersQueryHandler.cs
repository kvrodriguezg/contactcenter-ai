using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Users.DTOs;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Users.Queries.ListUsers;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListUsersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        if (_currentUserService.Role == Role.SuperAdmin)
        {
            // SuperAdmin: sin restricción por empresa.
        }
        else if (_currentUserService.CompanyId is not null)
        {
            query = query.Where(u => u.CompanyId == _currentUserService.CompanyId);
        }
        else
        {
            return [];
        }

        return await query
            .Include(u => u.Company)
            .OrderBy(u => u.Email)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CompanyId = u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null,
                AuthenticationProvider = u.AuthenticationProvider.ToString(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
