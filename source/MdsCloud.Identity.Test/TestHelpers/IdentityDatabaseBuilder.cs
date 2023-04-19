using FluentMigrator.Runner;
using MdsCloud.DbTooling.Commands;
using MdsCloud.Identity.Infrastructure.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using Npgsql;

namespace MdsCloud.Identity.Test.TestHelpers;

public class IdentityDatabaseBuilder : IDisposable
{
    public string TestDbConnectionString { get; private set; }
    public ISessionFactory? TestDbSessionFactory { get; private set; }
    private NpgsqlDataSource? DataSource { get; set; }
    private string RootConnectionString { get; set; }
    private string DbName { get; set; }

    ~IdentityDatabaseBuilder()
    {
        Dispose(false);
    }

    private IConfigurationRoot GetConfigRoot()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile("appsettings.Development.json", true, false)
            .AddJsonFile("appsettings.Production.json", true, false)
            .Build();
    }

    private async Task CreateAndSetupTestDatabase()
    {
        var config = GetConfigRoot();
        RootConnectionString = config["ConnectionStrings:DBConnection"];
        DbName = $"identity_{RandomStringGenerator.GenerateString(8)}".ToLowerInvariant();

        DataSource = NpgsqlDataSource.Create(RootConnectionString);
        var cmd = DataSource.CreateCommand($"CREATE DATABASE {DbName};");
        Console.WriteLine($"Creating database: {DbName}");
        await cmd.ExecuteNonQueryAsync();
        cmd.Dispose();

        TestDbConnectionString =
            $"Server=localhost;Port=5432;Database={DbName};User Id=postgres;Password=pwd4postgres;";
        await using var services = Migrations.CreateServices("Identity", TestDbConnectionString);
        using var scope = services.CreateScope();

        var runner = services.GetRequiredService<IMigrationRunner>();
        var migrationTask = Task.Run(() => runner.MigrateUp());

        var fluentConfig = NhibernateConfigGenerator.Generate(
            config,
            new Dictionary<string, string>
            {
                { "DBConnection", TestDbConnectionString },
                { "DeveloperSettings:ShowSql", "False" }
            }
        );
        TestDbSessionFactory = fluentConfig.BuildSessionFactory();
        await migrationTask;
    }

    public async Task Initialize()
    {
        if (DataSource == null)
        {
            await CreateAndSetupTestDatabase();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            TestDbSessionFactory?.Dispose();
            if (DataSource != null)
            {
                Console.WriteLine($"Dropping database: {DbName}");
                using (
                    var cmd = DataSource.CreateCommand(
                        $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{DbName}'"
                    )
                )
                {
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = DataSource.CreateCommand($"DROP DATABASE {DbName};"))
                {
                    cmd.ExecuteNonQuery();
                }
                DataSource.Dispose();
                DataSource = null;
            }
        }
    }
}
