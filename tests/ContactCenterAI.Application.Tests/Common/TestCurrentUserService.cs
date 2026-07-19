using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Application.Tests.Common;

/// <summary>
/// Configurable <see cref="ICurrentUserService"/> stub for handler unit tests.
/// </summary>
public class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; } = Guid.NewGuid();

    public string? Email { get; set; }

    public Role? Role { get; set; }

    public Guid? CompanyId { get; set; }

    public bool IsAuthenticated { get; set; } = true;

    public string? AuthorizationFailureMessage { get; set; }

    public static TestCurrentUserService AsSuperAdmin() => new()
    {
        Role = Domain.Identity.Role.SuperAdmin,
        Email = "superadmin@test.com"
    };

    public static TestCurrentUserService AsCompanyAdmin(Guid companyId) => new()
    {
        Role = Domain.Identity.Role.CompanyAdmin,
        CompanyId = companyId,
        Email = "companyadmin@test.com"
    };

    public static TestCurrentUserService AsAgent(Guid companyId) => new()
    {
        Role = Domain.Identity.Role.Agent,
        CompanyId = companyId,
        Email = "agent@test.com"
    };
}
