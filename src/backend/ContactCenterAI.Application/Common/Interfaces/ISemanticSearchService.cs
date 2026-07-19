using ContactCenterAI.Application.Documents.DTOs;

namespace ContactCenterAI.Application.Common.Interfaces;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticSearchResultDto>> SearchSimilarChunksAsync(
        Guid companyId,
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}
