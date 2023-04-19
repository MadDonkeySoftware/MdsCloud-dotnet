using MdsCloud.Identity.Business.DTOs;

namespace MdsCloud.Identity.Business.Interfaces;

public interface ITokenService
{
    public string GenerateUserToken(ArgsWithTrace<GenerateUserTokenArgs> tokenRequest);

    public string GenerateImpersonationToken(
        ArgsWithTrace<GenerateImpersonationTokenArgs> impersonationRequest
    );
}
