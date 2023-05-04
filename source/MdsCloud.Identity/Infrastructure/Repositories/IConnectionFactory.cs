using System.Data;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public interface IConnectionFactory
{
    IDbConnection GetConnection();
    T WithConnection<T>(Func<IDbConnection, T> action);
}
