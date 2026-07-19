namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>Maps Core <c>CompanyDto</c>. Only report §15.5 fields are exposed.</summary>
public class Company
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
