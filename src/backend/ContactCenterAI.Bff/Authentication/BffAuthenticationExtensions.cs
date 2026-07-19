using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ContactCenterAI.Bff.Authentication;

/// <summary>
/// Registers JWT authentication for the BFF mirroring Core/Chat exactly:
/// dual-mode Local (symmetric) or Auth0 (RSA256 + JWKS), with
/// <c>MapInboundClaims = false</c> and Authority <c>https://{Domain}/</c>.
/// </summary>
public static class BffAuthenticationExtensions
{
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authenticationSettings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);
        var auth0Settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);

        services.AddSingleton(authenticationSettings);
        services.AddSingleton(auth0Settings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (authenticationSettings.IsAuth0)
                {
                    ConfigureAuth0(options, auth0Settings, environment);
                }
                else
                {
                    ConfigureLocal(options, configuration);
                }
            });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureLocal(JwtBearerOptions options, IConfiguration configuration)
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

    private static void ConfigureAuth0(
        JwtBearerOptions options,
        Auth0Settings auth0Settings,
        IHostEnvironment environment)
    {
        if (!auth0Settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "AUTH_PROVIDER=Auth0 requiere AUTH0_DOMAIN y AUTH0_AUDIENCE configurados.");
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
}
