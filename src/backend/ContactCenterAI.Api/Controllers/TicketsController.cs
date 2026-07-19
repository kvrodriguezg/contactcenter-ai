using ContactCenterAI.Application.Tickets.Commands.AssignTicket;
using ContactCenterAI.Application.Tickets.Commands.ChangeTicketStatus;
using ContactCenterAI.Application.Tickets.Commands.CreateTicket;
using ContactCenterAI.Application.Tickets.Commands.ResolveTicket;
using ContactCenterAI.Application.Tickets.Queries.GetTicketById;
using ContactCenterAI.Application.Tickets.Queries.ListTickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListTicketsQuery(status, priority), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicketById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTicketCommand(
            request.Subject,
            request.Description,
            request.Priority,
            request.ConversationId,
            request.CompanyId);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignTicket(
        Guid id,
        [FromBody] AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignTicketCommand(id, request.AssignedToUserId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeTicketStatus(
        Guid id,
        [FromBody] ChangeTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeTicketStatusCommand(id, request.Status),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveTicket(
        Guid id,
        [FromBody] ResolveTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResolveTicketCommand(id, request.Resolution),
            cancellationToken);
        return Ok(result);
    }
}

public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Ignored for tenant users; CompanyId is always taken from the authenticated user.
    /// If sent and differs from the caller's company, the request is rejected.
    /// </summary>
    public Guid? CompanyId { get; set; }
}

public class AssignTicketRequest
{
    public Guid AssignedToUserId { get; set; }
}

public class ChangeTicketStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ResolveTicketRequest
{
    public string Resolution { get; set; } = string.Empty;
}
