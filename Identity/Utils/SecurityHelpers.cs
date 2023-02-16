using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Utils;

public static class SecurityHelpers
{
    public static TokenValidationParameters GetJwtValidationParameters(
        IConfiguration configuration,
        RSA rsa
    )
    {
        return new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidIssuer = configuration["MdsSettings:JwtSettings:Issuer"] ?? "mdsCloud",
            ValidateAudience = true,
            ValidAudience = configuration["MdsSettings:JwtSettings:Audience"] ?? "mdsCloud",
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuerSigningKey = true,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }
}
