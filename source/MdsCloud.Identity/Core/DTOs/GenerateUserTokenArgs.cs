namespace MdsCloud.Identity.Core.DTOs;

public class GenerateUserTokenArgs
{
    public virtual string AccountId { get; set; } = string.Empty;

    public virtual string UserId { get; set; } = string.Empty;

    public virtual string Password { get; set; } = string.Empty;
}
