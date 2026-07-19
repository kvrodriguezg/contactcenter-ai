using ContactCenterAI.Application.Companies.Queries.GetCompanyById;
using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Companies;

public class GetCompanyByIdQueryTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"get-company-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task Returns_company_for_super_admin()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new GetCompanyByIdQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(new GetCompanyByIdQuery(company.Id), CancellationToken.None);

        Assert.Equal("Acme", result.Name);
    }

    [Fact]
    public async Task Non_super_admin_cannot_read_other_company()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new GetCompanyByIdQueryHandler(context, TestCurrentUserService.AsCompanyAdmin(Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GetCompanyByIdQuery(company.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Missing_company_throws_not_found()
    {
        await using var context = CreateContext();
        var handler = new GetCompanyByIdQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new GetCompanyByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
