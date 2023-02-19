#pragma warning disable CS8618
namespace Identity.Domain;

public class User
{
    public virtual string Id { get; set; }
    public virtual string Email { get; set; } = "";
    public virtual string FriendlyName { get; set; } = "";
    public virtual string Password { get; set; } = "";
    public virtual bool IsPrimary { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual string? ActivationCode { get; set; }
    public virtual DateTime Created { get; set; }
    public virtual DateTime LastActivity { get; set; }
    public virtual DateTime LastModified { get; set; }
    public virtual Account Account { get; set; }
}
