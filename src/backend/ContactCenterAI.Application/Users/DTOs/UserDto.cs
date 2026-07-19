namespace ContactCenterAI.Application.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public string AuthenticationProvider { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
