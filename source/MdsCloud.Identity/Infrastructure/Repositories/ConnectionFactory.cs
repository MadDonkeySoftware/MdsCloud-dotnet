using System.Data;
using Npgsql;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public class ConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public ConnectionFactory(string? connectionString)
    {
        _connectionString =
            connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public T WithConnection<T>(Func<IDbConnection, T> func)
    {
        using var connection = this.GetConnection();
        connection.Open();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        return func(connection);
    }
}
