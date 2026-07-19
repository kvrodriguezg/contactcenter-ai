using ContactCenterAI.Application.Users.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;
