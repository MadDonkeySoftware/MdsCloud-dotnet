using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace MdsCloud.Identity.Utils;

public interface IRequestUtilities
{
    void Delay(int milliseconds);

    JwtSecurityToken GetRequestJwt(string authorizationHeader);
}
