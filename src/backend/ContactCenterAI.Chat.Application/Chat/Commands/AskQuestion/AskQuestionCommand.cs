using ContactCenterAI.Chat.Application.Chat.DTOs;
using MediatR;

namespace ContactCenterAI.Chat.Application.Chat.Commands.AskQuestion;

public record AskQuestionCommand(
    string Question,
    Guid? ConversationId,
    int TopK,
    string BearerToken) : IRequest<AskQuestionResponse>;
