using System.Security.Claims;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Identity;

public interface ILocalUserResolver
{
    Task<LocalUserResolution> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}

public sealed class LocalUserResolution
{
    public bool Succeeded { get; init; }

    public Guid? UserId { get; init; }

    public string? Email { get; init; }

    public Role? Role { get; init; }

    public Guid? CompanyId { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static LocalUserResolution Success(User user) => new()
    {
        Succeeded = true,
        UserId = user.Id,
        Email = user.Email,
        Role = user.Role,
        CompanyId = user.CompanyId
    };

    public static LocalUserResolution Fail(string errorCode, string errorMessage) => new()
    {
        Succeeded = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

public static class LocalUserResolutionErrors
{
    public const string NotAuthenticated = "not_authenticated";
    public const string MissingSubject = "missing_subject";
    public const string UserNotRegistered = "user_not_registered";
    public const string UserInactive = "user_inactive";
    public const string InvalidLocalSubject = "invalid_local_subject";
}

public class LocalUserResolver : ILocalUserResolver
{
    private readonly IApplicationDbContext _context;
    private readonly AuthenticationSettings _authenticationSettings;

    public LocalUserResolver(
        IApplicationDbContext context,
        IOptions<AuthenticationSettings> authenticationSettings)
    {
        _context = context;
        _authenticationSettings = authenticationSettings.Value;
    }

    public Task<LocalUserResolution> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(LocalUserResolution.Fail(
                LocalUserResolutionErrors.NotAuthenticated,
                "Usuario no autenticado."));
        }

        if (_authenticationSettings.IsAuth0)
        {
            return ResolveAuth0Async(principal, cancellationToken);
        }

        return ResolveLocalAsync(principal, cancellationToken);
    }

    private async Task<LocalUserResolution> ResolveLocalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
        {
            return LocalUserResolution.Fail(
                LocalUserResolutionErrors.InvalidLocalSubject,
                "El token local no contiene un identificador de usuario válido.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return await ValidateAndTouchAsync(user, forceSave: false, cancellationToken);
    }

    private async Task<LocalUserResolution> ResolveAuth0Async(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return LocalUserResolution.Fail(
                LocalUserResolutionErrors.MissingSubject,
                "El token de Auth0 no contiene el claim sub.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalSubject == subject, cancellationToken);

        var associatedExternalSubject = false;

        if (user is null)
        {
            var email = ResolveEmail(principal);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

                if (user is not null)
                {
                    user.ExternalSubject = subject;
                    user.AuthenticationProvider = AuthenticationProvider.Auth0;
                    associatedExternalSubject = true;
                }
            }
        }

        return await ValidateAndTouchAsync(user, associatedExternalSubject, cancellationToken);
    }

    private async Task<LocalUserResolution> ValidateAndTouchAsync(
        User? user,
        bool forceSave,
        CancellationToken cancellationToken)
    {
        if (user is null)
        {
            return LocalUserResolution.Fail(
                LocalUserResolutionErrors.UserNotRegistered,
                "El usuario autenticado no está registrado en ContactCenterAI.");
        }

        if (!user.IsActive)
        {
            return LocalUserResolution.Fail(
                LocalUserResolutionErrors.UserInactive,
                "El usuario está inactivo.");
        }

        var shouldUpdateLogin = user.LastLoginAt is null
            || user.LastLoginAt < DateTime.UtcNow.AddMinutes(-15);

        if (forceSave || shouldUpdateLogin)
        {
            if (shouldUpdateLogin)
            {
                user.LastLoginAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return LocalUserResolution.Success(user);
    }

    private static string? ResolveEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
    }
}
