using Dapper;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public UserRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool UserWithNameExists(string name)
    {
        const string sql =
            @"
SELECT count(1)
FROM ""user""
WHERE id = @name";

        return _connectionFactory.WithConnection(
                conn => conn.ExecuteScalar<long>(sql, new { name })
            ) > 0;
    }

    public User GetById(string id)
    {
        const string sql =
            @"
SELECT *
FROM ""user""
WHERE id = @id";

        return _connectionFactory.WithConnection(
            conn => conn.QueryFirstOrDefault<User>(sql, new { id })
        );
    }

    public User GetPrimaryUser(long accountId)
    {
        const string sql =
            @"
SELECT *
FROM ""user""
WHERE account_id = @accountId AND is_primary = true";

        return _connectionFactory.WithConnection(
            conn => conn.QueryFirstOrDefault<User>(sql, new { accountId })
        );
    }

    public void SaveUser(User item)
    {
        const string sql =
            @$"
INSERT INTO ""user"" (
                  id,
                  email,
                  account_id,
                  friendly_name,
                  password,
                  is_primary,
                  is_active,
                  activation_code,
                  created,
                  last_activity,
                  last_modified)
VALUES (
        @{nameof(item.Id)},
        @{nameof(item.Email)},
        @{nameof(item.AccountId)},
        @{nameof(item.FriendlyName)},
        @{nameof(item.Password)},
        @{nameof(item.IsPrimary)},
        @{nameof(item.IsActive)},
        @{nameof(item.ActivationCode)},
        @{nameof(item.Created)},
        @{nameof(item.LastActivity)},
        @{nameof(item.LastModified)})
ON CONFLICT (id) DO UPDATE
    SET email = @{nameof(item.Email)},
        account_id = @{nameof(item.AccountId)},
        friendly_name = @{nameof(item.FriendlyName)},
        password = @{nameof(item.Password)},
        is_primary = @{nameof(item.IsPrimary)},
        is_active = @{nameof(item.IsActive)},
        activation_code = @{nameof(item.ActivationCode)},
        created = @{nameof(item.Created)},
        last_activity = @{nameof(item.LastActivity)},
        last_modified = @{nameof(item.LastModified)};";

        _connectionFactory.WithConnection(conn => conn.Execute(sql, item));
    }
}
