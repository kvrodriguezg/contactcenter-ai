using System.Text;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using ContactCenterAI.Chat.Infrastructure.Ai;
using ContactCenterAI.Chat.Infrastructure.CoreApi;
using ContactCenterAI.Chat.Infrastructure.Identity;
using ContactCenterAI.Chat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContactCenterAI.Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authenticationSettings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);
        var auth0Settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);
        var coreApiSettings = configuration.GetSection(CoreApiSettings.SectionName).Get<CoreApiSettings>()
            ?? new CoreApiSettings();

        var coreBaseUrl = configuration["CoreApi__BaseUrl"]
            ?? configuration["CORE_API_BASE_URL"]
            ?? coreApiSettings.BaseUrl;

        coreApiSettings.BaseUrl = coreBaseUrl.TrimEnd('/');

        services.AddSingleton(authenticationSettings);
        services.AddSingleton(auth0Settings);
        services.AddSingleton(Options.Create(authenticationSettings));
        services.AddSingleton(Options.Create(auth0Settings));
        services.Configure<CoreApiSettings>(options =>
        {
            options.BaseUrl = coreApiSettings.BaseUrl;
            options.TimeoutSeconds = coreApiSettings.TimeoutSeconds;
        });
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var chatConnection = configuration.GetConnectionString("ChatDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:ChatDatabase es obligatorio para Chat API.");

        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(chatConnection));

        services.AddScoped<IChatDbContext>(provider =>
            provider.GetRequiredService<ChatDbContext>());

        services.AddHttpClient<IUserProfileClient, UserProfileClient>(client =>
        {
            client.BaseAddress = new Uri(coreApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(coreApiSettings.TimeoutSeconds);
        });

        services.AddHttpClient<IDocumentSearchClient, DocumentSearchClient>(client =>
        {
            client.BaseAddress = new Uri(coreApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(coreApiSettings.TimeoutSeconds);
        });

        services.AddHttpClient<IChatCompletionService, GeminiChatCompletionService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        return services;
    }

    public static IServiceCollection AddChatAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authenticationSettings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (authenticationSettings.IsAuth0)
                {
                    var auth0Settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);
                    if (!auth0Settings.IsConfigured)
                    {
                        throw new InvalidOperationException(
                            "AUTH_PROVIDER=Auth0 requiere AUTH0_DOMAIN y AUTH0_AUDIENCE.");
                    }

                    options.Authority = auth0Settings.Authority;
                    options.Audience = auth0Settings.Audience;
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = auth0Settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = auth0Settings.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RequireSignedTokens = true,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                    };
                }
                else
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
            });

        services.AddAuthorization();
        return services;
    }
}
