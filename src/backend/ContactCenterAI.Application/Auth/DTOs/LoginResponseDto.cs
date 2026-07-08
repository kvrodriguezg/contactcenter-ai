namespace ContactCenterAI.Application.Auth.DTOs;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }
}
