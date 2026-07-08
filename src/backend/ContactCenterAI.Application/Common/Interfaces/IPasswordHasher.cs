using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string passwordHash, string password);
}
