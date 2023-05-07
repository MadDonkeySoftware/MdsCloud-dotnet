using MdsCloud.Identity.Core.Model;

namespace MdsCloud.Identity.Core.Interfaces;

public interface IUserRepository
{
    bool UserWithNameExists(string name);
    User GetById(string id);
    User GetPrimaryUser(long accountId);
    void SaveUser(User user);
}
