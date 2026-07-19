namespace ContactCenterAI.Bff.Authentication;

/// <summary>
/// Same shape as Core/Chat <c>Auth0Settings</c> — read from the same env vars
/// (AUTH0_DOMAIN / AUTH0_AUDIENCE) so validation is identical across services.
/// </summary>
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
