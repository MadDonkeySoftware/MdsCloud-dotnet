using System.IdentityModel.Tokens.Jwt;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Utils;

namespace MdsCloud.Identity.Test.TestHelpers;

public class TestRequestUtilities : IRequestUtilities
{
    private readonly IRequestUtilities _utilities;

    public TestRequestUtilities(ISettings settings, IFile file)
    {
        _utilities = new RequestUtilities(settings, file);
    }

    public void Delay(int milliseconds)
    {
        // Do nothing so tests are quick.
    }

    public JwtSecurityToken GetRequestJwt(string authorizationHeader)
    {
        return _utilities.GetRequestJwt(authorizationHeader);
    }
}
