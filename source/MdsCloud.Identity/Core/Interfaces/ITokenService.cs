using MdsCloud.Identity.Core.DTOs;

namespace MdsCloud.Identity.Core.Interfaces;

public interface ITokenService
{
    public string GenerateUserToken(ArgsWithTrace<GenerateUserTokenArgs> tokenRequest);

    public string GenerateImpersonationToken(
        ArgsWithTrace<GenerateImpersonationTokenArgs> impersonationRequest
    );
}
