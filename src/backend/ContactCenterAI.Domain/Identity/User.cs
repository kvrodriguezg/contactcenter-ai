using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Tenancy;

namespace ContactCenterAI.Domain.Identity;

public class User : AuditableEntity
{
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<Document> UploadedDocuments { get; set; } = [];
}
