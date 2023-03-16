using System.Globalization;
using System.Reflection;
using CommandDotNet;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using MdsCloud.DbTooling.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scriban;

namespace MdsCloud.DbTooling.Commands;

public class Migrations
{
    private static ServiceProvider CreateServices(string scope)
    {
        var configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile("appsettings.Development.json", true, true)
            .AddJsonFile("appsettings.Production.json", true, true)
            .Build();
        var connString = configRoot[$"databases:{scope}:connString"];

        Console.WriteLine($"ConnString: {connString}");

        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(
                runner =>
                    runner
                        .AddPostgres()
                        .WithGlobalConnectionString(connString)
                        .ScanIn(Assembly.GetExecutingAssembly())
                        .For.Migrations()
            )
            .AddLogging(config => config.AddFluentMigratorConsole())
            .Configure<RunnerOptions>(conf => conf.Tags = new[] { scope })
            .BuildServiceProvider(false);
    }

    private void RunMigrations(string system, int attempts = 0)
    {
        try
        {
            using var serviceProvider = CreateServices(system);
            using var scope = serviceProvider.CreateScope();

            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
        catch (Exception)
        {
            if (attempts >= 5)
                throw;

            Thread.Sleep((int)Math.Pow(2, attempts) * 1000);
            RunMigrations(system, attempts + 1);
        }
    }

    [Command("run")]
    public void Run(string system, [Option("delay")] int delay = 0)
    {
        if (delay > 0)
        {
            // NOTE: Due to an issue in FluentMigrator if the database is not fully ready for
            // migrations to be run the process will exit w/o an exceptions or any indication that migrations
            // were not run.
            Console.WriteLine($"Sleeping for {delay} seconds before attempting migrations");
            Thread.Sleep(delay * 1000);
        }
        Console.WriteLine($"Running migrations for {system}");
        RunMigrations(system);
        Console.WriteLine($"Migrations for {system} have completed");
    }

    [Command("create")]
    public void Create(
        [Operand] string name,
        [Option('s', "service")] string? service = null,
        [Option('p', "path")] string? path = null,
        [Option('d', "description")] string? description = null
    )
    {
        var outPath = path ?? Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        var outService = service ?? "TODO";
        var outDescription = description ?? "TODO";

        var epochTimestamp = DateTime.Now.ToUnixTimestamp().ToString(CultureInfo.InvariantCulture);
        var fileName = $"{epochTimestamp}_{name}.cs";

        const string templateKey = "MdsCloud.DbTooling.Templates.Migration.scriban";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(templateKey);
        if (stream == null)
            throw new Exception($"Embedded resource missing: {templateKey}");

        using var reader = new StreamReader(stream);
        var templateBody = reader.ReadToEnd();

        var template = Template.Parse(templateBody);
        var rendered = template.Render(
            new
            {
                epoch = epochTimestamp,
                name,
                description = outDescription,
                service = outService
            }
        );

        File.WriteAllText(Path.Join(outPath, fileName), rendered);
        Console.WriteLine($"Migration {fileName} created at {outPath}");
    }
}
