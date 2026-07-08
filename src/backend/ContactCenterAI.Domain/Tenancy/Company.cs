using ContactCenterAI.Domain.Common;
using ContactCenterAI.Domain.Identity;

namespace ContactCenterAI.Domain.Tenancy;

public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public ICollection<User> Users { get; set; } = [];
}
