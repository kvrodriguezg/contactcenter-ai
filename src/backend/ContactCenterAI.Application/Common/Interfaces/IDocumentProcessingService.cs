namespace ContactCenterAI.Application.Common.Interfaces;

public interface IDocumentProcessingService
{
    Task<int> ProcessPendingDocumentsAsync(CancellationToken cancellationToken = default);
}
