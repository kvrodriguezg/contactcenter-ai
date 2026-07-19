using ContactCenterAI.Application.Documents.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Documents.Queries.ListDocuments;

public record ListDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;
