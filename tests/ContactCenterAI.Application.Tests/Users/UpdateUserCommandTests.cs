using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Application.Users.Commands.UpdateUser;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Users;

public class UpdateUserCommandTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"update-user-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task SuperAdmin_changes_role_status_and_company()
    {
        await using var context = CreateContext();
        var companyA = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var companyB = new Company { Id = Guid.NewGuid(), Name = "B", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = companyA.Id,
            AuthenticationProvider = AuthenticationProvider.Local,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.AddRange(companyA, companyB);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, nameof(Role.CompanyAdmin), false, companyB.Id),
            CancellationToken.None);

        Assert.Equal(nameof(Role.CompanyAdmin), result.Role);
        Assert.False(result.IsActive);
        Assert.Equal(companyB.Id, result.CompanyId);
    }

    [Fact]
    public async Task Update_sets_name_when_provided()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            AuthenticationProvider = AuthenticationProvider.Local,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, nameof(Role.Agent), true, company.Id, "Updated Name"),
            CancellationToken.None);

        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task Update_preserves_existing_name_when_not_provided()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.com",
            Name = "Original Name",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            AuthenticationProvider = AuthenticationProvider.Local,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, nameof(Role.Agent), true, company.Id),
            CancellationToken.None);

        Assert.Equal("Original Name", result.Name);
    }

    [Fact]
    public async Task Update_preserves_external_subject_for_auth0_user()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "auth0@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            ExternalSubject = "auth0|abc",
            AuthenticationProvider = AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        await handler.Handle(
            new UpdateUserCommand(user.Id, nameof(Role.Agent), false, company.Id),
            CancellationToken.None);

        var stored = await context.Users.SingleAsync();
        Assert.Equal("auth0|abc", stored.ExternalSubject);
        Assert.Equal(AuthenticationProvider.Auth0, stored.AuthenticationProvider);
    }

    [Fact]
    public async Task CompanyAdmin_cannot_edit_user_in_other_company()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            AuthenticationProvider = AuthenticationProvider.Local,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new UpdateUserCommand(user.Id, nameof(Role.Agent), true, company.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Missing_user_throws_not_found()
    {
        await using var context = CreateContext();
        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new UpdateUserCommand(Guid.NewGuid(), nameof(Role.SuperAdmin), true, null), CancellationToken.None));
    }
}
