using ContactCenterAI.Application.Documents.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Documents.Queries.SearchDocuments;

public record SearchDocumentsQuery(
    string Query,
    int TopK = 5,
    Guid? CompanyId = null) : IRequest<IReadOnlyList<SemanticSearchResultDto>>;
