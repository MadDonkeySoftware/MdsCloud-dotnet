using System.IdentityModel.Tokens.Jwt;

namespace MdsCloud.Identity.Business.DTOs;

public class UpdateUserDataArgs
{
    public JwtSecurityToken? RequestingUserJwt { get; set; }
    public virtual string? OldPassword { get; set; }
    public virtual string? NewPassword { get; set; }
    public virtual string? FriendlyName { get; set; }
    public virtual string? Email { get; set; }
}
