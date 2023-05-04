using FluentMigrator.Runner;
using MdsCloud.DbTooling.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MdsCloud.Identity.Test.TestHelpers;

public class IdentityDatabaseBuilder : IDisposable
{
    public string TestDbConnectionString { get; private set; }
    private NpgsqlDataSource? DataSource { get; set; }
    private string RootConnectionString { get; set; }
    public string DbName { get; private set; }

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
