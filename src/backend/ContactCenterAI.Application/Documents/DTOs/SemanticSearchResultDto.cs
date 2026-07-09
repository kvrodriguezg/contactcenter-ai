namespace ContactCenterAI.Application.Documents.DTOs;

public class SemanticSearchResultDto
{
    public Guid DocumentId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string ContentPreview { get; set; } = string.Empty;

    public double Score { get; set; }
}
