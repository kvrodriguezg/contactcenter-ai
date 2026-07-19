namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>Maps Core <c>DocumentDto</c>. StoragePath and raw chunks are not exposed.</summary>
public class Document
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
