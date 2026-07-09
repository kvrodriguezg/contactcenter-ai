namespace ContactCenterAI.Application.Chat.DTOs;

public class ConversationDetailDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<ConversationMessageDto> Messages { get; set; } = [];
}
