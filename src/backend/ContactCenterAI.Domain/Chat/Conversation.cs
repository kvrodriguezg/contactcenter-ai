using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;

namespace ContactCenterAI.Domain.Chat;

public class Conversation : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
