using ContactCenterAI.Chat.Application.Common.Interfaces;

namespace ContactCenterAI.Chat.Application.Chat;

public static class UserProfileValidator
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SuperAdmin",
        "CompanyAdmin",
        "Agent"
    };

    public static void EnsureValidForChat(UserProfileDto profile)
    {
        if (!profile.IsActive)
        {
            throw new UnauthorizedAccessException("El usuario está inactivo.");
        }

        if (string.IsNullOrWhiteSpace(profile.Role) || !ValidRoles.Contains(profile.Role))
        {
            throw new UnauthorizedAccessException("El usuario no tiene un rol válido.");
        }

        if (profile.CompanyId is null || profile.CompanyId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "El usuario no tiene empresa asignada. No puede usar Chat.");
        }

        if (profile.UserId == Guid.Empty || string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new UnauthorizedAccessException("Perfil de usuario incompleto.");
        }
    }
}
