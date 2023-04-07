using System.Security.Cryptography;
using MdsCloud.Identity.Settings;
using Microsoft.IdentityModel.Tokens;

namespace MdsCloud.Identity.Utils;

public static class SecurityHelpers
{
    public static TokenValidationParameters GetJwtValidationParameters(ISettings settings, RSA rsa)
    {
        return new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidIssuer = settings["MdsSettings:JwtSettings:Issuer"] ?? "mdsCloud",
            ValidateAudience = true,
            ValidAudience = settings["MdsSettings:JwtSettings:Audience"] ?? "mdsCloud",
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuerSigningKey = true,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }
}
