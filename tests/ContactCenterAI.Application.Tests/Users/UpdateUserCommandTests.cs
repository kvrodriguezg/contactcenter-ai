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

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Local());

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

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Local());

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

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Local());

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

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Local());

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

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(Guid.NewGuid()), TestAuthProviderMode.Local());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new UpdateUserCommand(user.Id, nameof(Role.Agent), true, company.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Missing_user_throws_not_found()
    {
        await using var context = CreateContext();
        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Local());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new UpdateUserCommand(Guid.NewGuid(), nameof(Role.SuperAdmin), true, null), CancellationToken.None));
    }

    [Fact]
    public async Task Update_changes_external_subject_when_provided()
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
            ExternalSubject = "auth0|old",
            AuthenticationProvider = AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Auth0());

        var result = await handler.Handle(
            new UpdateUserCommand(
                user.Id,
                nameof(Role.Agent),
                true,
                company.Id,
                null,
                "  auth0|687d1234567890abcdef  "),
            CancellationToken.None);

        Assert.Equal("auth0|687d1234567890abcdef", result.ExternalSubject);

        var stored = await context.Users.SingleAsync();
        Assert.Equal("auth0|687d1234567890abcdef", stored.ExternalSubject);
        Assert.Equal(AuthenticationProvider.Auth0, stored.AuthenticationProvider);
    }

    [Fact]
    public async Task Update_rejects_external_subject_owned_by_another_user()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            ExternalSubject = "auth0|taken",
            AuthenticationProvider = AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };
        var target = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            ExternalSubject = "auth0|mine",
            AuthenticationProvider = AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.AddRange(owner, target);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Auth0());

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => handler.Handle(
                new UpdateUserCommand(target.Id, nameof(Role.Agent), true, company.Id, null, "auth0|taken"),
                CancellationToken.None));

        Assert.Contains(
            ex.Errors,
            e => e.ErrorMessage == "El ID de Auth0 ya está asociado a otro usuario.");

        var stored = await context.Users.SingleAsync(u => u.Id == target.Id);
        Assert.Equal("auth0|mine", stored.ExternalSubject);
    }

    [Fact]
    public async Task Update_allows_keeping_same_external_subject()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "A", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "same@test.com",
            Role = Role.Agent,
            IsActive = true,
            CompanyId = company.Id,
            ExternalSubject = "auth0|keep-me",
            AuthenticationProvider = AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), TestAuthProviderMode.Auth0());

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, nameof(Role.CompanyAdmin), true, company.Id, null, "auth0|keep-me"),
            CancellationToken.None);

        Assert.Equal("auth0|keep-me", result.ExternalSubject);
        Assert.Equal(nameof(Role.CompanyAdmin), result.Role);
    }
}
