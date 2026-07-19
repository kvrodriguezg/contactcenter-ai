namespace ContactCenterAI.Bff.Authentication;

/// <summary>
/// Local (symmetric HMAC-SHA256) JWT validation settings — identical to Core/Chat.
/// Only used when AUTH_PROVIDER=Local.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ContactCenterAI";

    public string Audience { get; set; } = "ContactCenterAI.Client";

    public string SecretKey { get; set; } = string.Empty;
}
