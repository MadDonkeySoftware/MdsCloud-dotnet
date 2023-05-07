#pragma warning disable CS8618
namespace MdsCloud.Identity.Core.Model;

public class Account
{
    public virtual void SetId(long id)
    {
        if (Id == 0)
        {
            Id = id;
        }
        else
        {
            throw new InvalidOperationException("Id cannot be updated once it has a value");
        }
    }

    public virtual long Id { get; protected set; }
    public virtual string Name { get; set; } = "";
    public virtual bool IsActive { get; set; }
    public virtual DateTime Created { get; set; }
    public virtual DateTime? LastActivity { get; set; }
}
