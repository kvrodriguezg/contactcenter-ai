using ContactCenterAI.Application.Chat.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Chat.Queries.GetConversationById;

public record GetConversationByIdQuery(Guid Id) : IRequest<ConversationDetailDto>;
