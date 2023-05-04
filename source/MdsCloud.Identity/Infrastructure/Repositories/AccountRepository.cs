using Dapper;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public AccountRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool AccountWithNameExists(string name)
    {
        const string sql =
            @"
SELECT count(1)
FROM account
WHERE name = @name";

        return _connectionFactory.WithConnection(
                conn => conn.ExecuteScalar<long>(sql, new { name })
            ) > 0;
    }

    public Account GetById(long id)
    {
        const string sql =
            @"
SELECT *
FROM account
WHERE id = @id";

        return _connectionFactory.WithConnection(
            conn => conn.QueryFirstOrDefault<Account>(sql, new { id })
        );
    }

    public void SaveAccount(Account item)
    {
        string sql;
        if (item.Id == 0)
        {
            sql =
                @$"
INSERT INTO account (
    id,
    name,
    is_active,
    created,
    last_activity)
VALUES (
    nextval('account_pk_seq'),
    @{nameof(item.Name)},
    @{nameof(item.IsActive)},
    @{nameof(item.Created)},
    @{nameof(item.LastActivity)})
RETURNING id;";
        }
        else
        {
            sql =
                @$"
UPDATE account
SET name = @{nameof(item.Name)},
    is_active = @{nameof(item.IsActive)},
    created = @{nameof(item.Created)},
    last_activity = @{nameof(item.LastActivity)}
WHERE id = @{nameof(item.Id)}";
        }

        var id = _connectionFactory.WithConnection(conn => conn.ExecuteScalar<long?>(sql, item));
        if (id != null && item.Id == 0)
        {
            item.SetId(id.Value);
        }
    }
}
