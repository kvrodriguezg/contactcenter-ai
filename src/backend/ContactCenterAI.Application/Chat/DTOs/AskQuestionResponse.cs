namespace ContactCenterAI.Application.Chat.DTOs;

public class AskQuestionResponse
{
    public string Answer { get; set; } = string.Empty;

    public Guid ConversationId { get; set; }

    public IReadOnlyList<ChatSourceDto> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
