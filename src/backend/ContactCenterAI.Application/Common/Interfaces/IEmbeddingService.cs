namespace ContactCenterAI.Application.Common.Interfaces;

public interface IEmbeddingService
{
    bool IsConfigured { get; }

    Task<float[]> GenerateEmbeddingAsync(
        string text,
        string taskType = "RETRIEVAL_DOCUMENT",
        CancellationToken cancellationToken = default);
}
