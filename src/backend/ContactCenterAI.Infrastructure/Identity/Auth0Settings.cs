namespace ContactCenterAI.Infrastructure.Identity;

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
