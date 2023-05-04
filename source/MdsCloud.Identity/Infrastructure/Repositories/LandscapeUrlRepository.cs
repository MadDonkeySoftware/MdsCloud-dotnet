using Dapper;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public class LandscapeUrlRepository : ILandscapeUrlRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public LandscapeUrlRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IList<LandscapeUrl> GetLandscapeUrlsForScope(string scope)
    {
        const string sql =
            @"
SELECT *
FROM landscape_url
WHERE scope = @scope";

        return _connectionFactory.WithConnection<IList<LandscapeUrl>>(
            conn => conn.Query<LandscapeUrl>(sql, new { scope }).ToList()
        );
    }

    public void SaveLandscapeUrl(LandscapeUrl item)
    {
        const string sql =
            @$"
INSERT INTO landscape_url (scope, key, value)
VALUES (@{nameof(item.Scope)}, @{nameof(item.Key)}, @{nameof(item.Value)})
ON CONFLICT (scope, key) DO UPDATE SET value = @{nameof(item.Value)}";

        _connectionFactory.WithConnection(conn => conn.Execute(sql, item));
    }
}
