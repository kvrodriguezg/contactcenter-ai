using ContactCenterAI.Application.Chat.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Chat.Queries.ListConversations;

public record ListConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;
