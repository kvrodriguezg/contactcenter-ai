using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Application.Users.Commands.CreateUser;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Users;

public class CreateUserCommandTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"create-user-{Guid.NewGuid()}")
            .Options);

    private static Company SeedCompany(TestApplicationDbContext context, CompanyStatus status = CompanyStatus.Active)
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = status, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        context.SaveChanges();
        return company;
    }

    [Fact]
    public async Task SuperAdmin_creates_local_agent_with_hashed_password()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        var result = await handler.Handle(
            new CreateUserCommand("agent@test.com", nameof(Role.Agent), company.Id, "supersecret"),
            CancellationToken.None);

        Assert.Equal("agent@test.com", result.Email);
        Assert.Equal(nameof(Role.Agent), result.Role);
        Assert.Equal(nameof(AuthenticationProvider.Local), result.AuthenticationProvider);
        Assert.True(result.IsActive);

        var stored = await context.Users.SingleAsync();
        Assert.Equal("hashed::supersecret", stored.PasswordHash);
    }

    [Fact]
    public async Task SuperAdmin_creates_user_with_optional_name()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        var result = await handler.Handle(
            new CreateUserCommand("named@test.com", nameof(Role.Agent), company.Id, null, "  Jane Doe  "),
            CancellationToken.None);

        Assert.Equal("Jane Doe", result.Name);

        var stored = await context.Users.SingleAsync();
        Assert.Equal("Jane Doe", stored.Name);
    }

    [Fact]
    public async Task Name_is_null_when_not_provided()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        var result = await handler.Handle(
            new CreateUserCommand("noname@test.com", nameof(Role.Agent), company.Id, null),
            CancellationToken.None);

        Assert.Null(result.Name);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_case_insensitively()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "dup@test.com",
            Role = Role.Agent,
            CompanyId = company.Id,
            AuthenticationProvider = AuthenticationProvider.Local,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateUserCommand("DUP@test.com", nameof(Role.Agent), company.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task Company_must_exist()
    {
        await using var context = CreateContext();
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateUserCommand("a@test.com", nameof(Role.Agent), Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Company_must_be_active()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context, CompanyStatus.Inactive);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsSuperAdmin(), new FakePasswordHasher());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateUserCommand("a@test.com", nameof(Role.Agent), company.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task Agent_actor_cannot_create_users()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsAgent(company.Id), new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new CreateUserCommand("a@test.com", nameof(Role.Agent), company.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task CompanyAdmin_cannot_create_super_admin()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(company.Id), new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new CreateUserCommand("a@test.com", nameof(Role.SuperAdmin), null, null), CancellationToken.None));
    }

    [Fact]
    public async Task CompanyAdmin_cannot_create_user_in_other_company()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var handler = new CreateUserCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(company.Id), new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new CreateUserCommand("a@test.com", nameof(Role.Agent), Guid.NewGuid(), null), CancellationToken.None));
    }
}
