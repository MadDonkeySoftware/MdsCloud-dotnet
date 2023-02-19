using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

namespace Identity.Repo;

public static class NhibernateConfigGenerator
{
    public static FluentConfiguration Generate(
        IConfiguration config,
        Dictionary<string, string>? overrides = null
    )
    {
        overrides ??= new Dictionary<string, string>();

        var connString = overrides.ContainsKey("DBConnection")
            ? overrides["DBConnection"]
            : config.GetConnectionString("DBConnection");

        var dbConfig = PostgreSQLConfiguration.Standard.ConnectionString(connString);
        if (bool.TryParse(config["DeveloperSettings:ShowSql"], out var showSql) && showSql)
        {
            dbConfig.ShowSql();
        }

        return Fluently
            .Configure()
            .Database(dbConfig)
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Program>());
    }
}
