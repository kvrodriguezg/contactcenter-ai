using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Users.Common;

internal static class ExternalSubjectRules
{
    public const int MaxLength = 256;

    public const string RequiredMessage = "El ID de Auth0 es obligatorio cuando el proveedor de autenticación es Auth0.";

    public const string DuplicateMessage = "El ID de Auth0 ya está asociado a otro usuario.";

    public const string MaxLengthMessage = "El ID de Auth0 no puede superar los 256 caracteres.";

    /// <summary>Trims surrounding whitespace; empty/whitespace becomes <c>null</c>.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static void EnsureValidForCreate(
        IAuthProviderMode authProviderMode,
        string? normalizedExternalSubject,
        string propertyName)
    {
        if (authProviderMode.IsAuth0 && normalizedExternalSubject is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(propertyName, RequiredMessage)
            ]);
        }

        EnsureMaxLength(normalizedExternalSubject, propertyName);
    }

    public static void EnsureValidForUpdate(
        IAuthProviderMode authProviderMode,
        string? requestedExternalSubject,
        string? normalizedExternalSubject,
        string propertyName)
    {
        // null request means "leave unchanged" — do not require a value on that path.
        if (requestedExternalSubject is null)
        {
            return;
        }

        if (authProviderMode.IsAuth0 && normalizedExternalSubject is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(propertyName, RequiredMessage)
            ]);
        }

        EnsureMaxLength(normalizedExternalSubject, propertyName);
    }

    public static async Task EnsureUniqueAsync(
        IApplicationDbContext context,
        string? normalizedExternalSubject,
        Guid? excludeUserId,
        string propertyName,
        CancellationToken cancellationToken)
    {
        if (normalizedExternalSubject is null)
        {
            return;
        }

        var query = context.Users.AsNoTracking()
            .Where(u => u.ExternalSubject == normalizedExternalSubject);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        if (exists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(propertyName, DuplicateMessage)
            ]);
        }
    }

    public static void ApplyToUser(User user, string? normalizedExternalSubject)
    {
        user.ExternalSubject = normalizedExternalSubject;
        user.AuthenticationProvider = normalizedExternalSubject is null
            ? AuthenticationProvider.Local
            : AuthenticationProvider.Auth0;
    }

    private static void EnsureMaxLength(string? normalizedExternalSubject, string propertyName)
    {
        if (normalizedExternalSubject is not null && normalizedExternalSubject.Length > MaxLength)
        {
            throw new ValidationException(
            [
                new ValidationFailure(propertyName, MaxLengthMessage)
            ]);
        }
    }
}
