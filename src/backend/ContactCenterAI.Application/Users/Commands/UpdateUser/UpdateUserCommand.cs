using ContactCenterAI.Application.Users.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Users.Commands.UpdateUser;

/// <summary>
/// Updates role, active status and company assignment for a user.
/// <paramref name="Name"/> is optional: when omitted (null) the existing name is preserved;
/// pass an empty string to explicitly clear it.
/// <paramref name="ExternalSubject"/> is optional: when omitted (null) the existing Auth0
/// subject is preserved; pass a value to set/replace it (trimmed). Empty clears it when Local.
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    string Role,
    bool IsActive,
    Guid? CompanyId,
    string? Name = null,
    string? ExternalSubject = null) : IRequest<UserDto>;
