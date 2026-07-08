using ContactCenterAI.Application.Auth.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
