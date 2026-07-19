namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>
/// Result of an idempotent single-document processing attempt. Lets the caller (e.g. a message
/// consumer) log/observe what happened without inspecting exceptions for control flow.
/// </summary>
public enum DocumentProcessingOutcome
{
    /// <summary>The document id was not found in the database.</summary>
    NotFound = 0,

    /// <summary>The document was processed successfully during this call.</summary>
    Processed = 1,

    /// <summary>The document was already Processed; nothing to do (idempotent skip).</summary>
    SkippedAlreadyProcessed = 2,

    /// <summary>The document is currently being processed elsewhere; skipped to avoid duplicates.</summary>
    SkippedInProgress = 3,

    /// <summary>Processing was attempted but failed; the document is marked Failed.</summary>
    Failed = 4
}
