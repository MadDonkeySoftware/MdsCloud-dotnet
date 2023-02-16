#pragma warning disable CS8618
namespace Identity.Domain;

public class User
{
    public string Id { get; set; }
    public string Email { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public string? ActivationCode { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime LastModified { get; set; }
    public virtual Account Account { get; set; }
}
