using MdsCloud.Identity.Core.Model;

namespace MdsCloud.Identity.Core.Interfaces;

public interface IAccountRepository
{
    bool AccountWithNameExists(string name);
    Account GetById(long id);
    void SaveAccount(Account account);
}
