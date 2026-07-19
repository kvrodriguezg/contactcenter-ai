namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>Maps Core <c>GET /api/auth/me</c> (<c>CurrentUserDto</c>). No secrets exposed.</summary>
public class CurrentUser
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public bool IsActive { get; set; }
}
