namespace ContactCenterAI.Infrastructure.Documents;

public class DocumentProcessingSettings
{
    public const string SectionName = "DocumentProcessing";

    public int IntervalSeconds { get; set; } = 30;

    public int ChunkSize { get; set; } = 1000;

    public int ChunkOverlap { get; set; } = 150;

    public int BatchSize { get; set; } = 5;
}
