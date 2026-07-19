using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContactCenterAI.Chat.Application.Common;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Chat.Infrastructure.CoreApi;

public class DocumentSearchClient : IDocumentSearchClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentSearchClient> _logger;

    public DocumentSearchClient(HttpClient httpClient, ILogger<DocumentSearchClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentSearchHitDto>> SearchAsync(
        string bearerToken,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new
        {
            query,
            topK
        });

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Core API no disponible al consultar /api/documents/search");
            throw new ServiceUnavailableException(
                "El servicio de búsqueda documental no está disponible.",
                exception);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("No autorizado para buscar documentos.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new CoreApiException(404, "Endpoint de búsqueda documental no encontrado.");
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable
            || (int)response.StatusCode >= 500)
        {
            throw new ServiceUnavailableException(
                "El servicio de búsqueda documental no está disponible.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CoreApiException(
                (int)response.StatusCode,
                "No fue posible ejecutar la búsqueda documental.");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<CoreSearchHitResponse>>(
            cancellationToken: cancellationToken)
            ?? [];

        return payload.Select(hit => new DocumentSearchHitDto
        {
            DocumentId = hit.DocumentId,
            DocumentName = hit.DocumentName ?? hit.OriginalFileName ?? string.Empty,
            ChunkId = hit.ChunkId,
            Content = !string.IsNullOrWhiteSpace(hit.Content)
                ? hit.Content
                : hit.ContentPreview ?? string.Empty,
            Similarity = hit.Similarity != 0 ? hit.Similarity : hit.Score,
            PageNumber = hit.PageNumber,
            ChunkIndex = hit.ChunkIndex
        }).ToList();
    }

    private sealed class CoreSearchHitResponse
    {
        [JsonPropertyName("documentId")]
        public Guid DocumentId { get; set; }

        [JsonPropertyName("documentName")]
        public string? DocumentName { get; set; }

        [JsonPropertyName("originalFileName")]
        public string? OriginalFileName { get; set; }

        [JsonPropertyName("chunkId")]
        public Guid ChunkId { get; set; }

        [JsonPropertyName("chunkIndex")]
        public int ChunkIndex { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("contentPreview")]
        public string? ContentPreview { get; set; }

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("pageNumber")]
        public int? PageNumber { get; set; }
    }
}
