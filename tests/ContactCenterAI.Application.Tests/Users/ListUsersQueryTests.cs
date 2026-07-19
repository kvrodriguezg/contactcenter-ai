using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Application.Users.Queries.ListUsers;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Users;

public class ListUsersQueryTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"list-users-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task SuperAdmin_sees_all_users_with_company_and_provider_info()
    {
        await using var context = CreateContext();
        var companyA = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var companyB = new Company { Id = Guid.NewGuid(), Name = "B", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.AddRange(companyA, companyB);
        context.Users.AddRange(
            new User { Id = Guid.NewGuid(), Email = "a@test.com", Name = "Alice", Role = Role.Agent, CompanyId = companyA.Id, AuthenticationProvider = AuthenticationProvider.Local, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Email = "b@test.com", Role = Role.CompanyAdmin, CompanyId = companyB.Id, AuthenticationProvider = AuthenticationProvider.Auth0, ExternalSubject = "auth0|1", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var handler = new ListUsersQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        var alice = result.Single(u => u.Email == "a@test.com");
        Assert.Equal("Alice", alice.Name);
        Assert.Equal("A", alice.CompanyName);
        Assert.Equal(nameof(AuthenticationProvider.Local), alice.AuthenticationProvider);

        var bob = result.Single(u => u.Email == "b@test.com");
        Assert.Equal(nameof(AuthenticationProvider.Auth0), bob.AuthenticationProvider);
    }

    [Fact]
    public async Task CompanyAdmin_only_sees_users_from_own_company()
    {
        await using var context = CreateContext();
        var companyA = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var companyB = new Company { Id = Guid.NewGuid(), Name = "B", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.AddRange(companyA, companyB);
        context.Users.AddRange(
            new User { Id = Guid.NewGuid(), Email = "a@test.com", Role = Role.Agent, CompanyId = companyA.Id, AuthenticationProvider = AuthenticationProvider.Local, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Email = "b@test.com", Role = Role.Agent, CompanyId = companyB.Id, AuthenticationProvider = AuthenticationProvider.Local, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var handler = new ListUsersQueryHandler(context, TestCurrentUserService.AsCompanyAdmin(companyA.Id));

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("a@test.com", result[0].Email);
    }

    [Fact]
    public async Task User_without_company_sees_empty_list()
    {
        await using var context = CreateContext();
        var handler = new ListUsersQueryHandler(context, new TestCurrentUserService { Role = Role.Agent });

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
