using ContactCenterAI.Application.Documents.Commands.UploadDocument;
using ContactCenterAI.Application.Documents.Queries.GetDocumentById;
using ContactCenterAI.Application.Documents.Queries.ListDocuments;
using ContactCenterAI.Application.Documents.Queries.SearchDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new UploadDocumentCommand(
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.CompanyId);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListDocumentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocumentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDocumentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchDocuments(
        [FromBody] SearchDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new SearchDocumentsQuery(
            request.Query,
            request.TopK,
            request.CompanyId);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;

    public Guid? CompanyId { get; set; }
}

public class SearchDocumentsRequest
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;

    public Guid? CompanyId { get; set; }
}
