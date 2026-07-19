namespace ContactCenterAI.Chat.Application.Common.Interfaces;

public interface IUserProfileClient
{
    Task<UserProfileDto> GetCurrentUserAsync(
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public class UserProfileDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public bool IsActive { get; set; }
}
