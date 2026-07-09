using ContactCenterAI.Domain.Common;

namespace ContactCenterAI.Domain.Documents;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
