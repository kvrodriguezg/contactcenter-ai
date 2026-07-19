namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>Maps Core <c>UserDto</c>. PasswordHash / tokens are never part of the DTO.</summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public DateTime CreatedAt { get; set; }
}
