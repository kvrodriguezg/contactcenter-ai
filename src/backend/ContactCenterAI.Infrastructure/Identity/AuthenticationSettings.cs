namespace ContactCenterAI.Infrastructure.Identity;

public class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Proveedor activo: Local | Auth0
    /// </summary>
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
