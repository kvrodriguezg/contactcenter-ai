using System.Text.Json;
using ContactCenterAI.Chat.Application.Chat.DTOs;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Chat.Application.Chat.Queries.GetConversationById;

public record GetConversationByIdQuery(Guid Id, string BearerToken) : IRequest<ConversationDetailDto>;

public class GetConversationByIdQueryHandler
    : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatDbContext _dbContext;
    private readonly IUserProfileClient _userProfileClient;

    public GetConversationByIdQueryHandler(
        IChatDbContext dbContext,
        IUserProfileClient userProfileClient)
    {
        _dbContext = dbContext;
        _userProfileClient = userProfileClient;
    }

    public async Task<ConversationDetailDto> Handle(
        GetConversationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _userProfileClient.GetCurrentUserAsync(
            request.BearerToken,
            cancellationToken);

        UserProfileValidator.EnsureValidForChat(profile);

        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La conversación especificada no existe.");

        if (conversation.CompanyId != profile.CompanyId
            || conversation.ExternalUserId != profile.UserId)
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para acceder a esta conversación.");
        }

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            CompanyId = conversation.CompanyId,
            ExternalUserId = conversation.ExternalUserId,
            UserEmail = conversation.UserEmail,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ConversationMessageDto
                {
                    Id = m.Id,
                    Role = m.Role.ToString(),
                    Content = m.Content,
                    Sources = DeserializeSources(m.SourcesJson),
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };
    }

    private static IReadOnlyList<ChatSourceDto> DeserializeSources(string? sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ChatSourceDto>>(sourcesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
