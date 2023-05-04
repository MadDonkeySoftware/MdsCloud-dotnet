using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public interface IUserRepository
{
    bool UserWithNameExists(string name);
    User GetById(string id);
    User GetPrimaryUser(long accountId);
    void SaveUser(User user);
}
