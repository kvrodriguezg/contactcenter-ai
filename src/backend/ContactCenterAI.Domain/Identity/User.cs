using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Domain.Tickets;

namespace ContactCenterAI.Domain.Identity;

public class User : AuditableEntity
{
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public bool IsActive { get; set; } = true;

    public string? ExternalSubject { get; set; }

    public AuthenticationProvider AuthenticationProvider { get; set; } = AuthenticationProvider.Local;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<Document> UploadedDocuments { get; set; } = [];

    public ICollection<Chat.Conversation> Conversations { get; set; } = [];

    public ICollection<Ticket> CreatedTickets { get; set; } = [];

    public ICollection<Ticket> AssignedTickets { get; set; } = [];
}
