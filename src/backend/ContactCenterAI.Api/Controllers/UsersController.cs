using ContactCenterAI.Application.Users.Commands.CreateUser;
using ContactCenterAI.Application.Users.Commands.UpdateUser;
using ContactCenterAI.Application.Users.Queries.GetUserById;
using ContactCenterAI.Application.Users.Queries.ListUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.Email,
            request.Role,
            request.CompanyId,
            request.Password,
            request.Name,
            request.ExternalSubject);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            id,
            request.Role,
            request.IsActive,
            request.CompanyId,
            request.Name,
            request.ExternalSubject);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Role { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public string? Password { get; set; }

    /// <summary>Valor completo del claim Auth0 <c>sub</c> (p. ej. auth0|...).</summary>
    public string? ExternalSubject { get; set; }
}

public class UpdateUserRequest
{
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Guid? CompanyId { get; set; }

    public string? Name { get; set; }

    /// <summary>
    /// Valor completo del claim Auth0 <c>sub</c>. Omitir (<c>null</c>) para no modificarlo
    /// (p. ej. al cambiar solo el estado activo).
    /// </summary>
    public string? ExternalSubject { get; set; }
}
