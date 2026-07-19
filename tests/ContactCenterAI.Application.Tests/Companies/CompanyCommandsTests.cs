using ContactCenterAI.Application.Companies.Commands.CreateCompany;
using ContactCenterAI.Application.Companies.Commands.SetCompanyStatus;
using ContactCenterAI.Application.Companies.Commands.UpdateCompany;
using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Domain.Tenancy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Companies;

public class CompanyCommandsTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"companies-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task CreateCompany_as_super_admin_persists_active_company()
    {
        await using var context = CreateContext();
        var handler = new CreateCompanyCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(new CreateCompanyCommand("  Acme  "), CancellationToken.None);

        Assert.Equal("Acme", result.Name);
        Assert.Equal(nameof(CompanyStatus.Active), result.Status);
        Assert.Single(context.Companies);
    }

    [Fact]
    public async Task CreateCompany_without_super_admin_is_rejected()
    {
        await using var context = CreateContext();
        var handler = new CreateCompanyCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new CreateCompanyCommand("Acme"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateCompany_duplicate_name_is_case_insensitive_and_rejected()
    {
        await using var context = CreateContext();
        context.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var handler = new CreateCompanyCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateCompanyCommand("acme"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCompany_renames_and_changes_status()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Old", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new UpdateCompanyCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "New", nameof(CompanyStatus.Inactive)),
            CancellationToken.None);

        Assert.Equal("New", result.Name);
        Assert.Equal(nameof(CompanyStatus.Inactive), result.Status);
    }

    [Fact]
    public async Task UpdateCompany_duplicate_name_rejected_but_allows_same_company()
    {
        await using var context = CreateContext();
        var a = new Company { Id = Guid.NewGuid(), Name = "Alpha", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        var b = new Company { Id = Guid.NewGuid(), Name = "Beta", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.AddRange(a, b);
        await context.SaveChangesAsync();

        var handler = new UpdateCompanyCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new UpdateCompanyCommand(b.Id, "Alpha", nameof(CompanyStatus.Active)), CancellationToken.None));

        // Renaming to its own (case-different) name should succeed.
        var result = await handler.Handle(
            new UpdateCompanyCommand(a.Id, "ALPHA", nameof(CompanyStatus.Active)),
            CancellationToken.None);
        Assert.Equal("ALPHA", result.Name);
    }

    [Fact]
    public async Task UpdateCompany_missing_company_throws_not_found()
    {
        await using var context = CreateContext();
        var handler = new UpdateCompanyCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new UpdateCompanyCommand(Guid.NewGuid(), "X", nameof(CompanyStatus.Active)), CancellationToken.None));
    }

    [Fact]
    public async Task SetCompanyStatus_deactivates_company()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new SetCompanyStatusCommandHandler(context, TestCurrentUserService.AsSuperAdmin());

        var result = await handler.Handle(
            new SetCompanyStatusCommand(company.Id, CompanyStatus.Inactive),
            CancellationToken.None);

        Assert.Equal(nameof(CompanyStatus.Inactive), result.Status);
    }

    [Fact]
    public async Task SetCompanyStatus_without_super_admin_is_rejected()
    {
        await using var context = CreateContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new SetCompanyStatusCommandHandler(context, TestCurrentUserService.AsCompanyAdmin(company.Id));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(new SetCompanyStatusCommand(company.Id, CompanyStatus.Inactive), CancellationToken.None));
    }
}
