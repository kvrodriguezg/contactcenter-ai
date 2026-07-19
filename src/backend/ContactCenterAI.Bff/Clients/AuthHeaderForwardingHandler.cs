using Microsoft.AspNetCore.Http;

namespace ContactCenterAI.Bff.Clients;

/// <summary>
/// Delegating handler that copies the incoming <c>Authorization: Bearer &lt;token&gt;</c>
/// header onto every downstream REST call. This is what preserves multi-company
/// isolation and role enforcement: Core/Chat scope every response by the token's
/// user, so the BFF never needs (and must not attempt) to bypass tenant scoping.
/// </summary>
public sealed class AuthHeaderForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var incoming = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            request.Headers.TryAddWithoutValidation("Authorization", incoming);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
