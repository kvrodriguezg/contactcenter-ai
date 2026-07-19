using ContactCenterAI.Domain.Common;

namespace ContactCenterAI.Domain.Chat;

public class ConversationMessage : BaseEntity
{
    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? SourcesJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
