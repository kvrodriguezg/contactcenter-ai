using Microsoft.Extensions.Configuration;

namespace ContactCenterAI.Chat.Infrastructure.Identity;

public class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public string Provider { get; set; } = AuthenticationProviders.Local;

    public bool IsLocal =>
        string.Equals(Provider, AuthenticationProviders.Local, StringComparison.OrdinalIgnoreCase);

    public bool IsAuth0 =>
        string.Equals(Provider, AuthenticationProviders.Auth0, StringComparison.OrdinalIgnoreCase);
}

public static class AuthenticationProviders
{
    public const string Local = "Local";
    public const string Auth0 = "Auth0";
}

public class Auth0Settings
{
    public const string SectionName = "Auth0";

    public string Domain { get; set; } = string.Empty;

    public string Audience { get; set; } = "https://contactcenterai-api";

    public string Authority =>
        string.IsNullOrWhiteSpace(Domain)
            ? string.Empty
            : $"https://{Domain.Trim().TrimEnd('/')}/";

    public string Issuer => Authority;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Audience);
}

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}

public static class AuthenticationConfiguration
{
    public static AuthenticationSettings ResolveAuthenticationSettings(
        Microsoft.Extensions.Configuration.IConfiguration configuration)
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

    public static Auth0Settings ResolveAuth0Settings(
        Microsoft.Extensions.Configuration.IConfiguration configuration)
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
