using ContactCenterAI.Bff.GraphQL.Models;

namespace ContactCenterAI.Bff.Clients;

/// <summary>
/// Typed HTTP client for the Chat REST microservice (conversations + messages).
/// Forwards the bearer token; failures surface as <see cref="DownstreamApiException"/>.
/// </summary>
public interface IChatApiClient
{
    Task<IReadOnlyList<Conversation>> GetConversationsAsync(CancellationToken ct);

    Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct);
}
