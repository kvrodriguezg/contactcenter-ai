using System.Security.Claims;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using Microsoft.AspNetCore.Http;

namespace ContactCenterAI.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public Role? Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<Role>(role, out var parsedRole) ? parsedRole : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var companyId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("companyId");
            return Guid.TryParse(companyId, out var id) ? id : null;
        }
    }
}
