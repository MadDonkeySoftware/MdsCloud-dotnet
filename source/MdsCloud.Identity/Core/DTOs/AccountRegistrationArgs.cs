namespace MdsCloud.Identity.Core.DTOs;

public class AccountRegistrationArgs
{
    public virtual string AccountName { get; set; } = string.Empty;

    public virtual string Email { get; set; } = string.Empty;

    public virtual string FriendlyName { get; set; } = string.Empty;

    public virtual string Password { get; set; } = string.Empty;

    public virtual string UserId { get; set; } = string.Empty;
}
