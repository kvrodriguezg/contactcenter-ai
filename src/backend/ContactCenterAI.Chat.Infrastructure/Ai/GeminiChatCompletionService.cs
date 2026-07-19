using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using ContactCenterAI.Chat.Application.Common;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Chat.Infrastructure.Ai;

public class GeminiChatCompletionService : IChatCompletionService
{
    private const string SystemPrompt =
        "Eres un asistente de soporte para agentes de contact center. " +
        "Responde en español, de forma clara y profesional. " +
        "Usa exclusivamente el contexto entregado. " +
        "Si el contexto no contiene la respuesta, indica que no hay información suficiente en los documentos cargados. " +
        "No inventes información.";

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiChatCompletionService> _logger;

    public GeminiChatCompletionService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiChatCompletionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<string> contextChunks,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new ChatAiException("Proveedor de IA no configurado para generar respuestas de chat.");
        }

        var userPrompt = BuildUserPrompt(question, contextChunks);
        var requestUri = $"v1beta/models/{_settings.ChatModel}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = SystemPrompt }]
            },
            Contents =
            [
                new GeminiContentWithRole
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = userPrompt }]
                }
            ]
        });

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Error de comunicación con Gemini API");
            throw new ChatAiException("No fue posible generar la respuesta con el proveedor de IA.", exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Gemini API respondió con estado {StatusCode}",
                (int)response.StatusCode);
            throw new ChatAiException("No fue posible generar la respuesta con el proveedor de IA.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
            cancellationToken: cancellationToken);

        var answer = payload?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ChatAiException("El proveedor de IA no devolvió una respuesta válida.");
        }

        return answer.Trim();
    }

    private static string BuildUserPrompt(string question, IReadOnlyList<string> contextChunks)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Contexto recuperado de documentos:");

        if (contextChunks.Count == 0)
        {
            builder.AppendLine("(sin contexto disponible)");
        }
        else
        {
            for (var index = 0; index < contextChunks.Count; index++)
            {
                builder.AppendLine($"[{index + 1}] {contextChunks[index]}");
            }
        }

        builder.AppendLine();
        builder.Append("Pregunta del agente: ");
        builder.Append(question);
        return builder.ToString();
    }

    private sealed class GeminiGenerateContentRequest
    {
        [JsonPropertyName("systemInstruction")]
        public GeminiContent SystemInstruction { get; set; } = new();

        [JsonPropertyName("contents")]
        public GeminiContentWithRole[] Contents { get; set; } = [];
    }

    private sealed class GeminiContentWithRole
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("parts")]
        public GeminiPart[] Parts { get; set; } = [];
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

    private sealed class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
