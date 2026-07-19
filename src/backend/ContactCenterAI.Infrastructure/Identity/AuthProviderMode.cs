using ContactCenterAI.Application.Common.Interfaces;

namespace ContactCenterAI.Infrastructure.Identity;

public sealed class AuthProviderMode : IAuthProviderMode
{
    private readonly AuthenticationSettings _settings;

    public AuthProviderMode(AuthenticationSettings settings)
    {
        _settings = settings;
    }

    public bool IsAuth0 => _settings.IsAuth0;
}
