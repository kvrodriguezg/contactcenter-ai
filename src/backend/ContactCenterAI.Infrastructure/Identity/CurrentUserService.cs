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

    private LocalUserContext? LocalUser =>
        _httpContextAccessor.HttpContext?.Items[LocalUserContextKeys.Resolution] as LocalUserContext;

    public bool IsAuthenticated => LocalUser?.IsResolved == true;

    public Guid? UserId => LocalUser?.UserId;

    public string? Email => LocalUser?.Email;

    public Role? Role => LocalUser?.Role;

    public Guid? CompanyId => LocalUser?.CompanyId;

    public string? AuthorizationFailureMessage =>
        LocalUser is { IsResolved: false }
            ? LocalUser.Resolution.ErrorMessage
            : null;
}
