using System.Net;
using System.Net.Http.Json;
using ContactCenterAI.Bff.GraphQL.Models;

namespace ContactCenterAI.Bff.Clients;

public sealed class ChatApiClient : IChatApiClient
{
    private const string ServiceName = "ChatApi";

    private readonly HttpClient _http;

    public ChatApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<Conversation>> GetConversationsAsync(CancellationToken ct) =>
        await GetAsync<List<Conversation>>("api/chat/conversations", ct) ?? [];

    public async Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct)
    {
        var detail = await GetOrDefaultAsync<ConversationDetailDto>($"api/chat/conversations/{id}", ct);
        if (detail is null)
        {
            return null;
        }

        return new Conversation
        {
            Id = detail.Id,
            CompanyId = detail.CompanyId,
            ExternalUserId = detail.ExternalUserId,
            UserEmail = detail.UserEmail,
            Title = detail.Title,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            Messages = detail.Messages
        };
    }

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
            throw new DownstreamApiException(ServiceName, "Chat API no está disponible.", null, ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamApiException(ServiceName, "Chat API agotó el tiempo de espera.", null, ex);
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new DownstreamApiException(
                ServiceName,
                $"Chat API respondió con estado {(int)response.StatusCode}.",
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
            throw new DownstreamApiException(ServiceName, "Respuesta ilegible de Chat API.", null, ex);
        }
    }

    /// <summary>Private DTO matching Chat API conversation detail payload.</summary>
    private sealed class ConversationDetailDto
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public Guid ExternalUserId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public IReadOnlyList<ConversationMessage> Messages { get; set; } = [];
    }
}
