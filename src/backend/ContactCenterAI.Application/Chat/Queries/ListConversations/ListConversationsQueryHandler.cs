using ContactCenterAI.Application.Chat.DTOs;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Chat.Queries.ListConversations;

public class ListConversationsQueryHandler
    : IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListConversationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(
        ListConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("El usuario debe estar autenticado.");
        }

        var query = _context.Conversations.AsNoTracking();

        if (_currentUserService.Role == Role.SuperAdmin)
        {
            // SuperAdmin: sin restricción por empresa.
        }
        else if (_currentUserService.CompanyId is not null && _currentUserService.UserId is not null)
        {
            query = query.Where(conversation =>
                conversation.CompanyId == _currentUserService.CompanyId &&
                conversation.UserId == _currentUserService.UserId);
        }
        else
        {
            return [];
        }

        return await query
            .OrderByDescending(conversation => conversation.CreatedAt)
            .Select(conversation => new ConversationDto
            {
                Id = conversation.Id,
                CompanyId = conversation.CompanyId,
                CompanyName = conversation.Company.Name,
                UserId = conversation.UserId,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
