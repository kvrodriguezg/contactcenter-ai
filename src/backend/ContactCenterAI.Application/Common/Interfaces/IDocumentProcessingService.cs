namespace ContactCenterAI.Application.Common.Interfaces;

public interface IDocumentProcessingService
{
    /// <summary>
    /// Polling entry point: scans the database for pending documents and processes a batch.
    /// Used by the reconciliation/fallback loop.
    /// </summary>
    Task<int> ProcessPendingDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event-driven entry point: processes a single document by id with idempotency guarantees.
    /// Safe to call multiple times for the same document (e.g. duplicate messages + polling).
    /// </summary>
    Task<DocumentProcessingOutcome> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
