using ContactCenterAI.Application.Users.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Role,
    Guid? CompanyId,
    string? Password,
    string? Name = null,
    string? ExternalSubject = null) : IRequest<UserDto>;
