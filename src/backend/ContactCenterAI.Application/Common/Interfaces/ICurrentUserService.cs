using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    Role? Role { get; }

    Guid? CompanyId { get; }

    bool IsAuthenticated { get; }
}
