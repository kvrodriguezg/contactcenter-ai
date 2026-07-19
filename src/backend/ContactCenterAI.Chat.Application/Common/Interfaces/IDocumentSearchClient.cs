namespace ContactCenterAI.Chat.Application.Common.Interfaces;

public interface IDocumentSearchClient
{
    Task<IReadOnlyList<DocumentSearchHitDto>> SearchAsync(
        string bearerToken,
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}

public class DocumentSearchHitDto
{
    public Guid DocumentId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public Guid ChunkId { get; set; }

    public string Content { get; set; } = string.Empty;

    public double Similarity { get; set; }

    public int? PageNumber { get; set; }

    public int ChunkIndex { get; set; }
}
