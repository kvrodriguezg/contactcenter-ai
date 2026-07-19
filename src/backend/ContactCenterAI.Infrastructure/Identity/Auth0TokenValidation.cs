using Microsoft.IdentityModel.Tokens;

namespace ContactCenterAI.Infrastructure.Identity;

public static class Auth0TokenValidation
{
    public static TokenValidationParameters Create(Auth0Settings settings)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "Auth0 no está configurado. Defina AUTH0_DOMAIN y AUTH0_AUDIENCE.");
        }

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };
    }
}
