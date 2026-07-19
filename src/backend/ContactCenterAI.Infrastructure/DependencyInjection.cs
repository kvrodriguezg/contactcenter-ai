using System.Text;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Infrastructure.Identity;
using ContactCenterAI.Infrastructure.Persistence;
using ContactCenterAI.Infrastructure.Storage;
using ContactCenterAI.Infrastructure.Documents;
using ContactCenterAI.Infrastructure.Ai;
using ContactCenterAI.Infrastructure.Chat;
using ContactCenterAI.Infrastructure.Tickets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContactCenterAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        var authenticationSettings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);
        var auth0Settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);

        services.AddSingleton(authenticationSettings);
        services.AddSingleton(auth0Settings);
        services.AddSingleton(Options.Create(authenticationSettings));
        services.AddSingleton(Options.Create(auth0Settings));

        var chatServiceSettings = ChatServiceConfiguration.Resolve(configuration);
        services.AddSingleton(chatServiceSettings);
        services.AddSingleton(Options.Create(chatServiceSettings));

        services.Configure<DocumentStorageSettings>(
            configuration.GetSection(DocumentStorageSettings.SectionName));

        services.Configure<DocumentProcessingSettings>(
            configuration.GetSection(DocumentProcessingSettings.SectionName));

        services.Configure<AiSettings>(
            configuration.GetSection(AiSettings.SectionName));

        services.Configure<GeminiSettings>(
            configuration.GetSection(GeminiSettings.SectionName));

        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IChatCompletionService, GeminiChatCompletionService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });

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
        services.AddScoped<ILocalUserResolver, LocalUserResolver>();
        services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<ITicketEventPublisher, NoOpTicketEventPublisher>();
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        return services;
    }

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var authenticationSettings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (authenticationSettings.IsAuth0)
                {
                    ConfigureAuth0JwtBearer(options, configuration, environment);
                }
                else
                {
                    ConfigureLocalJwtBearer(options, configuration);
                }
            });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureLocalJwtBearer(
        JwtBearerOptions options,
        IConfiguration configuration)
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
    }

    private static void ConfigureAuth0JwtBearer(
        JwtBearerOptions options,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var auth0Settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);

        if (!auth0Settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "AUTH_PROVIDER=Auth0 requiere AUTH0_DOMAIN y AUTH0_AUDIENCE configurados.");
        }

        options.Authority = auth0Settings.Authority;
        options.Audience = auth0Settings.Audience;
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.TokenValidationParameters = Auth0TokenValidation.Create(auth0Settings);

        // Mapear claim "sub" de forma estable para resolución de perfil local.
        options.MapInboundClaims = false;
    }
}
