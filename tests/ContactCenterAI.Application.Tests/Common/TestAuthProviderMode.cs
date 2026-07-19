using ContactCenterAI.Application.Common.Interfaces;

namespace ContactCenterAI.Application.Tests.Common;

public class TestAuthProviderMode : IAuthProviderMode
{
    public bool IsAuth0 { get; init; }

    public static TestAuthProviderMode Local() => new() { IsAuth0 = false };

    public static TestAuthProviderMode Auth0() => new() { IsAuth0 = true };
}
