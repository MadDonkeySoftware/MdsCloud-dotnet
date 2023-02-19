using System.Globalization;
using System.Reflection;
using CommandDotNet;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scriban;

namespace DbTooling.Commands;

public class Migrations
{
    private static ServiceProvider CreateServices(string scope)
    {
        var configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .Build();
        var connString = configRoot[$"databases:{scope}:connString"];

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
            .Configure<RunnerOptions>(conf => conf.Tags = new[] { "Identity" })
            .BuildServiceProvider(false);
    }

    [Command("run")]
    public void Run(string system)
    {
        using var serviceProvider = CreateServices(system);
        using var scope = serviceProvider.CreateScope();

        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
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

        const string templateKey = "DbTooling.Templates.Migration.scriban";
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
