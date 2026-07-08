using ContactCenterAI.Application.Auth.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;
