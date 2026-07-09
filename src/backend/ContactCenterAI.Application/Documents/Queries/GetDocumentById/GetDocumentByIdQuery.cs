using ContactCenterAI.Application.Documents.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Documents.Queries.GetDocumentById;

public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto>;
