using ContactCenterAI.Application.Auth.Commands.Login;
using ContactCenterAI.Application.Auth.Queries.GetCurrentUser;
using ContactCenterAI.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AuthenticationSettings _authenticationSettings;

    public AuthController(
        IMediator mediator,
        IOptions<AuthenticationSettings> authenticationSettings)
    {
        _mediator = mediator;
        _authenticationSettings = authenticationSettings.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (_authenticationSettings.IsAuth0)
        {
            return StatusCode(
                StatusCodes.Status410Gone,
                new
                {
                    message = "El login local está deshabilitado. Use Auth0 para autenticarse."
                });
        }

        try
        {
            var result = await _mediator.Send(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
