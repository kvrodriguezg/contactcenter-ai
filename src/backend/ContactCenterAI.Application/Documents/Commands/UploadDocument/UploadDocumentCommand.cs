using ContactCenterAI.Application.Documents.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Documents.Commands.UploadDocument;

public record UploadDocumentCommand(
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Guid? CompanyId) : IRequest<DocumentDto>;
