using ContactCenterAI.Application.Companies.Queries.ListCompanies;
using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Companies;

public class ListCompaniesQueryTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"list-companies-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task SuperAdmin_sees_all_companies_ordered_by_name()
    {
        await using var context = CreateContext();
        context.Companies.AddRange(
            new Company { Id = Guid.NewGuid(), Name = "Beta", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow },
            new Company { Id = Guid.NewGuid(), Name = "Alpha", Status = CompanyStatus.Inactive, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var handler = new ListCompaniesQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(new ListCompaniesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task CompanyAdmin_only_sees_own_company()
    {
        await using var context = CreateContext();
        var companyA = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var companyB = new Company { Id = Guid.NewGuid(), Name = "B", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.AddRange(companyA, companyB);
        await context.SaveChangesAsync();

        var handler = new ListCompaniesQueryHandler(context, TestCurrentUserService.AsCompanyAdmin(companyA.Id));

        var result = await handler.Handle(new ListCompaniesQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }
}
