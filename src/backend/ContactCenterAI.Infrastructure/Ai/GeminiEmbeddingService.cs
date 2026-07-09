using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContactCenterAI.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Ai;

public class GeminiEmbeddingService : IEmbeddingService
{
    private const string NotConfiguredMessage =
        "Proveedor de IA no configurado para generar embeddings.";

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string taskType = "RETRIEVAL_DOCUMENT",
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(NotConfiguredMessage);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("El texto para generar embedding no puede estar vacío.", nameof(text));
        }

        var requestUri =
            $"v1beta/models/{_settings.EmbeddingsModel}:embedContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new GeminiEmbedContentRequest
        {
            Model = $"models/{_settings.EmbeddingsModel}",
            Content = new GeminiContent
            {
                Parts = [new GeminiPart { Text = text }]
            },
            TaskType = taskType,
            OutputDimensionality = _settings.EmbeddingDimensions
        });

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Error de comunicación con Gemini API al generar embedding");
            throw new InvalidOperationException("No fue posible generar el embedding con Gemini API.", exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini API respondió con estado {StatusCode}: {ErrorBody}",
                (int)response.StatusCode,
                errorBody);

            throw new InvalidOperationException("No fue posible generar el embedding con Gemini API.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiEmbedContentResponse>(
            cancellationToken: cancellationToken);

        var values = payload?.Embedding?.Values;

        if (values is null || values.Length == 0)
        {
            throw new InvalidOperationException("Gemini API no devolvió un embedding válido.");
        }

        if (values.Length != _settings.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"El embedding devuelto tiene {values.Length} dimensiones, se esperaban {_settings.EmbeddingDimensions}.");
        }

        return Normalize(values);
    }

    private static float[] Normalize(float[] vector)
    {
        double sumSquares = 0;

        foreach (var value in vector)
        {
            sumSquares += value * value;
        }

        if (sumSquares == 0)
        {
            return vector;
        }

        var norm = Math.Sqrt(sumSquares);
        var normalized = new float[vector.Length];

        for (var index = 0; index < vector.Length; index++)
        {
            normalized[index] = (float)(vector[index] / norm);
        }

        return normalized;
    }

    private sealed class GeminiEmbedContentRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        [JsonPropertyName("taskType")]
        public string TaskType { get; set; } = "RETRIEVAL_DOCUMENT";

        [JsonPropertyName("outputDimensionality")]
        public int OutputDimensionality { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[] Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiEmbedContentResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    private sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; } = [];
    }
}
