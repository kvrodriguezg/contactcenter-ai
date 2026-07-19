using ContactCenterAI.Domain.Documents;

namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>
/// Pure, side-effect-free idempotency rules for document processing. Extracted so the
/// "skip already Processed / in-progress" decision is unit-testable without a database.
/// </summary>
public static class DocumentProcessingRules
{
    /// <summary>
    /// Returns true when a document in the given state should be (re)processed. A document that
    /// is already <see cref="DocumentStatus.Processed"/> or currently
    /// <see cref="DocumentStatus.Processing"/> is skipped to avoid duplicate work/chunks.
    /// <see cref="DocumentStatus.Failed"/> is eligible for a fresh attempt.
    /// </summary>
    public static bool ShouldProcess(DocumentStatus status) => status switch
    {
        DocumentStatus.Uploaded => true,
        DocumentStatus.PendingProcessing => true,
        DocumentStatus.Failed => true,
        DocumentStatus.Processing => false,
        DocumentStatus.Processed => false,
        _ => false
    };

    /// <summary>Maps a "should not process" state to the corresponding skip outcome.</summary>
    public static DocumentProcessingOutcome SkipOutcomeFor(DocumentStatus status) => status switch
    {
        DocumentStatus.Processed => DocumentProcessingOutcome.SkippedAlreadyProcessed,
        DocumentStatus.Processing => DocumentProcessingOutcome.SkippedInProgress,
        _ => DocumentProcessingOutcome.SkippedAlreadyProcessed
    };
}
