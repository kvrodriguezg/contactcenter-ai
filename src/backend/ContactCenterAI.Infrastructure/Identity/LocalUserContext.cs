using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Infrastructure.Identity;

public static class LocalUserContextKeys
{
    public const string Resolution = "ContactCenterAI.LocalUserResolution";
}

public sealed class LocalUserContext
{
    public required LocalUserResolution Resolution { get; init; }

    public Guid? UserId => Resolution.Succeeded ? Resolution.UserId : null;

    public string? Email => Resolution.Succeeded ? Resolution.Email : null;

    public Role? Role => Resolution.Succeeded ? Resolution.Role : null;

    public Guid? CompanyId => Resolution.Succeeded ? Resolution.CompanyId : null;

    public bool IsResolved => Resolution.Succeeded;
}
