using System.IdentityModel.Tokens.Jwt;

namespace MdsCloud.Identity.UI.Utils;

public interface IRequestUtilities
{
    void Delay(int milliseconds);

    JwtSecurityToken GetRequestJwt(string authorizationHeader);
}
