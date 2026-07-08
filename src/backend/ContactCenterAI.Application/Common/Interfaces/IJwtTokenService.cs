using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(User user);
}
