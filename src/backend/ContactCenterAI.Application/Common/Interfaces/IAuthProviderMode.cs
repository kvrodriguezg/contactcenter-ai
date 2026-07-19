namespace ContactCenterAI.Application.Common.Interfaces;

/// <summary>
/// Exposes the active authentication provider (Local | Auth0) to Application use-cases
/// without coupling them to Infrastructure settings types.
/// </summary>
public interface IAuthProviderMode
{
    bool IsAuth0 { get; }
}
