namespace ContactCenterAI.Infrastructure.Ai;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string EmbeddingsModel { get; set; } = "gemini-embedding-001";

    public string ChatModel { get; set; } = "gemini-2.5-flash";

    public int EmbeddingDimensions { get; set; } = 1536;
}
