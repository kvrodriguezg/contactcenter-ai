using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Documents;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class DocumentProcessingRulesTests
{
    [Theory]
    [InlineData(DocumentStatus.Uploaded, true)]
    [InlineData(DocumentStatus.PendingProcessing, true)]
    [InlineData(DocumentStatus.Failed, true)]
    [InlineData(DocumentStatus.Processing, false)]
    [InlineData(DocumentStatus.Processed, false)]
    public void ShouldProcess_matches_idempotency_matrix(DocumentStatus status, bool expected)
    {
        Assert.Equal(expected, DocumentProcessingRules.ShouldProcess(status));
    }

    [Fact]
    public void Already_processed_document_maps_to_skipped_outcome()
    {
        Assert.Equal(
            DocumentProcessingOutcome.SkippedAlreadyProcessed,
            DocumentProcessingRules.SkipOutcomeFor(DocumentStatus.Processed));
    }

    [Fact]
    public void In_progress_document_maps_to_skipped_in_progress()
    {
        Assert.Equal(
            DocumentProcessingOutcome.SkippedInProgress,
            DocumentProcessingRules.SkipOutcomeFor(DocumentStatus.Processing));
    }
}
