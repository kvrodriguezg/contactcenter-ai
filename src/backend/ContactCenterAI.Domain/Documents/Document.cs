using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;

namespace ContactCenterAI.Domain.Documents;

public class Document : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }

    public User UploadedByUser { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
