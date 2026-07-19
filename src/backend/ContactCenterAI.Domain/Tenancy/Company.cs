using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tickets;

namespace ContactCenterAI.Domain.Tenancy;

public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public ICollection<User> Users { get; set; } = [];

    public ICollection<Document> Documents { get; set; } = [];

    public ICollection<Chat.Conversation> Conversations { get; set; } = [];

    public ICollection<Ticket> Tickets { get; set; } = [];
}
