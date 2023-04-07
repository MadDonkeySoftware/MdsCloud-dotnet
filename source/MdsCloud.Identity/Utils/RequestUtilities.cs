using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Settings;

namespace MdsCloud.Identity.Utils;

public class RequestUtilities : IRequestUtilities
{
    private readonly IFile _file;
    private readonly ISettings _settings;

    public RequestUtilities(ISettings settings, IFile file)
    {
        _file = file;
        _settings = settings;
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

        var publicKeyText = _file.ReadAllText(_settings["MdsSettings:Secrets:PublicPath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyText);

        var validationParameters = SecurityHelpers.GetJwtValidationParameters(_settings, rsa);

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(standardizedAuthHeader, validationParameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwtValidatedToken)
            throw new Exception("Failed to coerce security token to jwt security token");
        return jwtValidatedToken;
    }
}
