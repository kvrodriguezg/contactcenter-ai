using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContactCenterAI.Chat.Application.Common;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Chat.Infrastructure.CoreApi;

public class UserProfileClient : IUserProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserProfileClient> _logger;

    public UserProfileClient(HttpClient httpClient, ILogger<UserProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Core API no disponible al consultar /api/auth/me");
            throw new ServiceUnavailableException(
                "El servicio Core no está disponible. Intente nuevamente más tarde.",
                exception);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("No autorizado para obtener el perfil de usuario.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new UnauthorizedAccessException("El usuario autenticado no está registrado en ContactCenterAI.");
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable
            || (int)response.StatusCode >= 500)
        {
            throw new ServiceUnavailableException(
                "El servicio Core no está disponible. Intente nuevamente más tarde.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CoreApiException(
                (int)response.StatusCode,
                "No fue posible obtener el perfil de usuario desde Core API.");
        }

        var payload = await response.Content.ReadFromJsonAsync<CoreCurrentUserResponse>(
            cancellationToken: cancellationToken)
            ?? throw new CoreApiException(502, "Respuesta inválida de Core API en /api/auth/me.");

        return new UserProfileDto
        {
            UserId = payload.UserId,
            Email = payload.Email,
            Role = payload.Role,
            CompanyId = payload.CompanyId,
            CompanyName = payload.CompanyName,
            IsActive = payload.IsActive
        };
    }

    private sealed class CoreCurrentUserResponse
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("companyId")]
        public Guid? CompanyId { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}
