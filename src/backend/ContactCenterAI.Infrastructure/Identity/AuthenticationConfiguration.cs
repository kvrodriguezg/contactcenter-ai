using Microsoft.Extensions.Configuration;

namespace ContactCenterAI.Infrastructure.Identity;

public static class AuthenticationConfiguration
{
    public static AuthenticationSettings ResolveAuthenticationSettings(IConfiguration configuration)
    {
        var settings = configuration.GetSection(AuthenticationSettings.SectionName)
            .Get<AuthenticationSettings>()
            ?? new AuthenticationSettings();

        var providerFromEnv = configuration["AUTH_PROVIDER"];
        if (!string.IsNullOrWhiteSpace(providerFromEnv))
        {
            settings.Provider = providerFromEnv.Trim();
        }

        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            settings.Provider = AuthenticationProviders.Local;
        }

        return settings;
    }

    public static Auth0Settings ResolveAuth0Settings(IConfiguration configuration)
    {
        var settings = configuration.GetSection(Auth0Settings.SectionName)
            .Get<Auth0Settings>()
            ?? new Auth0Settings();

        var domain = configuration["AUTH0_DOMAIN"];
        if (!string.IsNullOrWhiteSpace(domain))
        {
            settings.Domain = domain.Trim();
        }

        var audience = configuration["AUTH0_AUDIENCE"];
        if (!string.IsNullOrWhiteSpace(audience))
        {
            settings.Audience = audience.Trim();
        }

        return settings;
    }
}
