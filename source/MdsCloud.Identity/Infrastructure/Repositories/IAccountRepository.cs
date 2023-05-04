using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public interface IAccountRepository
{
    bool AccountWithNameExists(string name);
    Account GetById(long id);
    void SaveAccount(Account account);
}
