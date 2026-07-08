using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContactCenterAI.Infrastructure.Persistence;

public static class ApplicationDbSeeder
{
    private const string SeedCompanyName = "Empresa Telecomunicaciones Simulada";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();

        Company? company = null;

        if (!await context.Companies.AnyAsync(c => c.Name == SeedCompanyName, cancellationToken))
        {
            company = new Company
            {
                Id = Guid.NewGuid(),
                Name = SeedCompanyName,
                Status = CompanyStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            company = await context.Companies
                .FirstAsync(c => c.Name == SeedCompanyName, cancellationToken);
        }

        await SeedUserAsync(
            context,
            passwordHasher,
            email: "admin@contactcenterai.cl",
            password: "Admin123*",
            role: Role.SuperAdmin,
            companyId: null,
            cancellationToken);

        await SeedUserAsync(
            context,
            passwordHasher,
            email: "agente@contactcenterai.cl",
            password: "Agent123*",
            role: Role.Agent,
            companyId: company.Id,
            cancellationToken);
    }

    private static async Task SeedUserAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        string email,
        string password,
        Role role,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Role = role,
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
