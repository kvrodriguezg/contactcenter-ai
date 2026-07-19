namespace ContactCenterAI.Application.Documents.DTOs;

public class SemanticSearchResultDto
{
    public Guid DocumentId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public string ContentPreview { get; set; } = string.Empty;

    public double Similarity { get; set; }

    public double Score { get; set; }

    public int? PageNumber { get; set; }
}
