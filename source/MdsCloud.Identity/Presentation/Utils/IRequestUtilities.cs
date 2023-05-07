using System.IdentityModel.Tokens.Jwt;

namespace MdsCloud.Identity.Presentation.Utils;

public interface IRequestUtilities
{
    void Delay(int milliseconds);

    JwtSecurityToken GetRequestJwt(string authorizationHeader);
}
