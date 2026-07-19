using System.Net;
using System.Net.Http.Json;
using ContactCenterAI.Bff.GraphQL.Models;

namespace ContactCenterAI.Bff.Clients;

public sealed class CoreApiClient : ICoreApiClient
{
    private const string ServiceName = "CoreApi";

    private readonly HttpClient _http;

    public CoreApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken ct) =>
        GetOrDefaultAsync<CurrentUser>("api/auth/me", ct);

    public async Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken ct) =>
        await GetAsync<List<Company>>("api/Companies", ct) ?? [];

    public Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken ct) =>
        GetOrDefaultAsync<Company>($"api/Companies/{id}", ct);

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct) =>
        await GetAsync<List<User>>("api/Users", ct) ?? [];

    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct) =>
        GetOrDefaultAsync<User>($"api/Users/{id}", ct);

    public async Task<IReadOnlyList<Document>> GetDocumentsAsync(CancellationToken ct) =>
        await GetAsync<List<Document>>("api/Documents", ct) ?? [];

    public Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken ct) =>
        GetOrDefaultAsync<Document>($"api/Documents/{id}", ct);

    public async Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct) =>
        await GetAsync<List<Ticket>>("api/Tickets", ct) ?? [];

    public Task<Ticket?> GetTicketByIdAsync(Guid id, CancellationToken ct) =>
        GetOrDefaultAsync<Ticket>($"api/Tickets/{id}", ct);

    private async Task<T?> GetOrDefaultAsync<T>(string path, CancellationToken ct)
    {
        var response = await SendAsync(path, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        EnsureSuccess(response);
        return await DeserializeAsync<T>(response, ct);
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        var response = await SendAsync(path, ct);
        EnsureSuccess(response);
        return await DeserializeAsync<T>(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken ct)
    {
        try
        {
            return await _http.GetAsync(path, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new DownstreamApiException(ServiceName, "Core API no está disponible.", null, ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamApiException(ServiceName, "Core API agotó el tiempo de espera.", null, ex);
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new DownstreamApiException(
                ServiceName,
                $"Core API respondió con estado {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(BffJson.Options, ct);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
        {
            throw new DownstreamApiException(ServiceName, "Respuesta ilegible de Core API.", null, ex);
        }
    }
}
