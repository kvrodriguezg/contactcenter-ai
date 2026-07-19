namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Published after a document is successfully persisted. Consumed by the Worker to trigger
/// the existing extraction/chunking/embedding pipeline. Matches architecture report §15.4.
/// </summary>
public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid CompanyId,
    Guid UploadedByUserId,
    DateTime OccurredAtUtc);
