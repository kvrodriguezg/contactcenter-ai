using ContactCenterAI.Application.Chat.Commands.AskQuestion;
using ContactCenterAI.Application.Chat.Queries.GetConversationById;
using ContactCenterAI.Application.Chat.Queries.ListConversations;
using ContactCenterAI.Infrastructure.Chat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ChatServiceSettings _chatServiceSettings;

    public ChatController(
        IMediator mediator,
        IOptions<ChatServiceSettings> chatServiceSettings)
    {
        _mediator = mediator;
        _chatServiceSettings = chatServiceSettings.Value;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (_chatServiceSettings.IsExternal)
        {
            return Gone();
        }

        var command = new AskQuestionCommand(
            request.Question,
            request.ConversationId,
            request.TopK,
            request.CompanyId);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        if (_chatServiceSettings.IsExternal)
        {
            return Gone();
        }

        var result = await _mediator.Send(new ListConversationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversationById(Guid id, CancellationToken cancellationToken)
    {
        if (_chatServiceSettings.IsExternal)
        {
            return Gone();
        }

        var result = await _mediator.Send(new GetConversationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    private ObjectResult Gone() =>
        StatusCode(
            StatusCodes.Status410Gone,
            new
            {
                message = "Chat embebido deshabilitado. Use Chat API externa (CHAT_SERVICE_MODE=External)."
            });
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;

    public Guid? ConversationId { get; set; }

    public int TopK { get; set; } = 5;

    public Guid? CompanyId { get; set; }
}
