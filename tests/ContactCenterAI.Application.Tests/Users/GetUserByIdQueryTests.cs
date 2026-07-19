using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Application.Users.Queries.GetUserById;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Users;

public class GetUserByIdQueryTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"get-user-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task Returns_user_for_super_admin()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User { Id = Guid.NewGuid(), Email = "u@test.com", Name = "User One", Role = Role.Agent, CompanyId = company.Id, AuthenticationProvider = AuthenticationProvider.Local, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserByIdQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.Equal("u@test.com", result.Email);
        Assert.Equal("User One", result.Name);
        Assert.Equal("Acme", result.CompanyName);
    }

    [Fact]
    public async Task CompanyAdmin_cannot_read_user_from_other_company()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User { Id = Guid.NewGuid(), Email = "u@test.com", Role = Role.Agent, CompanyId = company.Id, AuthenticationProvider = AuthenticationProvider.Local, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserByIdQueryHandler(context, TestCurrentUserService.AsCompanyAdmin(Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Missing_user_throws_not_found()
    {
        await using var context = CreateContext();
        var handler = new GetUserByIdQueryHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
