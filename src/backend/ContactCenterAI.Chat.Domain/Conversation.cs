namespace ContactCenterAI.Chat.Domain;

public class Conversation
{
    public Guid Id { get; set; }

    public Guid ExternalUserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
