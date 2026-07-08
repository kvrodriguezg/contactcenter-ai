using ContactCenterAI.Application.Users.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Users.Queries.ListUsers;

public record ListUsersQuery : IRequest<IReadOnlyList<UserDto>>;
