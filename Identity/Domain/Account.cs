#pragma warning disable CS8618
namespace Identity.Domain;

public class Account
{
    public Account()
    {
        Users = new List<User>();
    }

    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public virtual List<User> Users { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastActivity { get; set; }
}
