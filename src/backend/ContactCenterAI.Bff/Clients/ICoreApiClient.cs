using ContactCenterAI.Bff.GraphQL.Models;

namespace ContactCenterAI.Bff.Clients;

/// <summary>
/// Typed HTTP client for the Core REST API. Every call forwards the caller's
/// bearer token (via <see cref="AuthHeaderForwardingHandler"/>) so Core enforces
/// tenant/role scoping. Failures surface as <see cref="DownstreamApiException"/>.
/// </summary>
public interface ICoreApiClient
{
    Task<CurrentUser?> GetCurrentUserAsync(CancellationToken ct);

    Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken ct);

    Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct);

    Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Document>> GetDocumentsAsync(CancellationToken ct);

    Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct);

    Task<Ticket?> GetTicketByIdAsync(Guid id, CancellationToken ct);
}
