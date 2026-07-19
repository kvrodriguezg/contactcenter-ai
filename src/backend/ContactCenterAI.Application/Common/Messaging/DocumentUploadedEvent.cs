namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Published after a document is successfully persisted. Consumed by the Worker to trigger
/// the existing extraction/chunking/embedding pipeline.
/// </summary>
public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid CompanyId,
    Guid UploadedByUserId,
    DateTime OccurredAt,
    Guid CorrelationId);
