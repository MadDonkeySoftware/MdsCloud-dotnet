using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace MdsCloud.Identity.Utils;

public class RequestUtilities : IRequestUtilities
{
    private readonly IConfiguration _configuration;

    public RequestUtilities(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Delay(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    public JwtSecurityToken GetRequestJwt(string authorizationHeader)
    {
        const string prefix = "bearer ";
        var standardizedAuthHeader = authorizationHeader.ToLowerInvariant().StartsWith(prefix)
            ? authorizationHeader.Substring(prefix.Length)
            : authorizationHeader;

        var publicKeyText = File.ReadAllText(
            _configuration["MdsSettings:Secrets:PublicPath"] ?? ""
        );
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyText);

        var validationParameters = SecurityHelpers.GetJwtValidationParameters(_configuration, rsa);

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(standardizedAuthHeader, validationParameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwtValidatedToken)
            throw new Exception("Failed to coerce security token to jwt security token");
        return jwtValidatedToken;
    }
}
