using System.IdentityModel.Tokens.Jwt;

namespace MdsCloud.Identity.Core.DTOs;

public class GenerateImpersonationTokenArgs
{
    public JwtSecurityToken? RequestingUserJwt { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string? UserId { get; set; }
}
