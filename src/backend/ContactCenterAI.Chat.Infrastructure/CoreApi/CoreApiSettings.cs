namespace ContactCenterAI.Chat.Infrastructure.CoreApi;

public class CoreApiSettings
{
    public const string SectionName = "CoreApi";

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public int TimeoutSeconds { get; set; } = 30;
}
