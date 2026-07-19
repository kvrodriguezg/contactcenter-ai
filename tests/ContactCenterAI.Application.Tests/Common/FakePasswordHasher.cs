using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Application.Tests.Common;

public class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(User user, string password) => $"hashed::{password}";

    public bool VerifyPassword(User user, string passwordHash, string password) =>
        passwordHash == $"hashed::{password}";
}
