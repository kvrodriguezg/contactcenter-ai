namespace ContactCenterAI.Chat.Infrastructure.Ai;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gemini-2.5-flash";
}
