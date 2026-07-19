namespace ContactCenterAI.Bff.Authentication;

/// <summary>
/// Mirrors the Core/Chat authentication provider selection so the BFF validates
/// tokens exactly like the rest of the platform (dual-mode Local | Auth0).
/// </summary>
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
