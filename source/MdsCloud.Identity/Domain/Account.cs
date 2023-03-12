using MdsCloud.Identity.Repo.CustomTypes;

#pragma warning disable CS8618
namespace MdsCloud.Identity.Domain;

public class Account
{
    public Account()
    {
        Users = new List<User>();
    }

    public virtual long Id { get; protected set; }
    public virtual string Name { get; set; } = "";
    public virtual IList<User> Users { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual DateTime Created { get; set; }
    public virtual DateTime? LastActivity { get; set; }
}
