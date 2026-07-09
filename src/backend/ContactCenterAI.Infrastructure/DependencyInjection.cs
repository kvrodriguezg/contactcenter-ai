using System.Text;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Infrastructure.Identity;
using ContactCenterAI.Infrastructure.Persistence;
using ContactCenterAI.Infrastructure.Storage;
using ContactCenterAI.Infrastructure.Documents;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ContactCenterAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.Configure<DocumentStorageSettings>(
            configuration.GetSection(DocumentStorageSettings.SectionName));

        services.Configure<DocumentProcessingSettings>(
            configuration.GetSection(DocumentProcessingSettings.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseVector());
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

        return services;
    }

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                    ?? new JwtSettings();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
