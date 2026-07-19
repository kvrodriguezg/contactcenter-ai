using ContactCenterAI.Application.Chat.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Chat.Commands.AskQuestion;

public record AskQuestionCommand(
    string Question,
    Guid? ConversationId = null,
    int TopK = 5,
    Guid? CompanyId = null) : IRequest<AskQuestionResponse>;
