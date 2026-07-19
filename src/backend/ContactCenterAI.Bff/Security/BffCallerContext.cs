using ContactCenterAI.Bff.Clients;
using ContactCenterAI.Bff.GraphQL.Models;
using HotChocolate;

namespace ContactCenterAI.Bff.Security;

public static class BffRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string Agent = "Agent";
}

/// <summary>
/// Resolves the authenticated caller via Core <c>/api/auth/me</c> and enforces
/// BFF-level role / tenant rules on top of downstream REST scoping.
/// </summary>
public sealed class BffCallerContext
{
    private readonly ICoreApiClient _core;

    public BffCallerContext(ICoreApiClient core)
    {
        _core = core;
    }

    public async Task<CurrentUser> RequireCallerAsync(CancellationToken ct)
    {
        var me = await _core.GetCurrentUserAsync(ct);
        if (me is null || !me.IsActive)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("No autenticado o usuario inactivo.")
                    .SetCode("AUTH_REQUIRED")
                    .Build());
        }

        return me;
    }

    public static bool IsSuperAdmin(CurrentUser user) =>
        string.Equals(user.Role, BffRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsCompanyAdmin(CurrentUser user) =>
        string.Equals(user.Role, BffRoles.CompanyAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsAgent(CurrentUser user) =>
        string.Equals(user.Role, BffRoles.Agent, StringComparison.OrdinalIgnoreCase);

    public static void EnsureSuperAdmin(CurrentUser user)
    {
        if (!IsSuperAdmin(user))
        {
            throw Forbidden("Solo SuperAdmin puede consultar el listado global de empresas.");
        }
    }

    public static void EnsureCanListUsers(CurrentUser user)
    {
        if (IsAgent(user))
        {
            throw Forbidden("Agent no puede consultar listados administrativos de usuarios.");
        }

        if (!IsSuperAdmin(user) && !IsCompanyAdmin(user))
        {
            throw Forbidden("No tiene permisos para listar usuarios.");
        }
    }

    public static void EnsureCanAccessCompany(CurrentUser user, Guid companyId)
    {
        if (IsSuperAdmin(user))
        {
            return;
        }

        if (user.CompanyId is null || user.CompanyId != companyId)
        {
            throw Forbidden("No tiene acceso a esta empresa.");
        }
    }

    public static IReadOnlyList<Company> FilterCompanies(CurrentUser user, IReadOnlyList<Company> companies)
    {
        if (IsSuperAdmin(user))
        {
            return companies;
        }

        if (user.CompanyId is null)
        {
            return [];
        }

        return companies.Where(c => c.Id == user.CompanyId).ToList();
    }

    public static IReadOnlyList<User> FilterUsers(CurrentUser user, IReadOnlyList<User> users)
    {
        if (IsSuperAdmin(user))
        {
            return users;
        }

        if (user.CompanyId is null)
        {
            return [];
        }

        return users.Where(u => u.CompanyId == user.CompanyId).ToList();
    }

    public static IReadOnlyList<T> FilterByCompany<T>(
        CurrentUser user,
        IReadOnlyList<T> items,
        Func<T, Guid> companyIdSelector)
    {
        if (IsSuperAdmin(user))
        {
            return items;
        }

        if (user.CompanyId is null)
        {
            return [];
        }

        var companyId = user.CompanyId.Value;
        return items.Where(i => companyIdSelector(i) == companyId).ToList();
    }

    private static GraphQLException Forbidden(string message) =>
        new(ErrorBuilder.New()
            .SetMessage(message)
            .SetCode("FORBIDDEN")
            .Build());
}
