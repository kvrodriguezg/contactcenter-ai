namespace ContactCenterAI.Application.Companies.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
