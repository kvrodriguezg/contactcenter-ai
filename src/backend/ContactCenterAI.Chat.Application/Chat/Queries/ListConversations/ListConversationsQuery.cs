using System.Text.Json;
using ContactCenterAI.Chat.Application.Chat.DTOs;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Chat.Application.Chat.Queries.ListConversations;

public record ListConversationsQuery(string BearerToken) : IRequest<IReadOnlyList<ConversationDto>>;

public class ListConversationsQueryHandler
    : IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IChatDbContext _dbContext;
    private readonly IUserProfileClient _userProfileClient;

    public ListConversationsQueryHandler(
        IChatDbContext dbContext,
        IUserProfileClient userProfileClient)
    {
        _dbContext = dbContext;
        _userProfileClient = userProfileClient;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(
        ListConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _userProfileClient.GetCurrentUserAsync(
            request.BearerToken,
            cancellationToken);

        UserProfileValidator.EnsureValidForChat(profile);

        var companyId = profile.CompanyId!.Value;

        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.ExternalUserId == profile.UserId)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                ExternalUserId = c.ExternalUserId,
                UserEmail = c.UserEmail,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
