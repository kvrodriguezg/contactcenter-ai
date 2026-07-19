namespace ContactCenterAI.Infrastructure.Documents;

public class DocumentProcessingSettings
{
    public const string SectionName = "DocumentProcessing";

    public int IntervalSeconds { get; set; } = 30;

    public int ChunkSize { get; set; } = 1000;

    public int ChunkOverlap { get; set; } = 150;

    public int BatchSize { get; set; } = 5;

    /// <summary>
    /// Configurable polling fallback. When true (default) the Worker keeps scanning the DB for
    /// pending documents. When RabbitMQ is the primary path, set this false to rely on events, or
    /// keep it true with a longer <see cref="IntervalSeconds"/> so polling acts as reconciliation
    /// for anything missed during a broker outage.
    /// </summary>
    public bool PollingEnabled { get; set; } = true;
}
