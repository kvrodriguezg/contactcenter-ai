namespace ContactCenterAI.Application.Chat.DTOs;

public class ConversationMessageDto
{
    public Guid Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<ChatSourceDto> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
