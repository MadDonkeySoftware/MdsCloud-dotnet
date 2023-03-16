using CommandDotNet;
using MdsCloud.CLI.Attributes;
using MdsCloud.CLI.Domain;
using MdsCloud.CLI.Stack;
using MdsCloud.CLI.Utils;
using MdsCloud.SdkDotNet;
using MdsCloud.SdkDotNet.Utils;
using Newtonsoft.Json;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

public class Stack : BaseMdsCommand
{
    private string BaseStackConfigDirectory { get; }

    public Stack(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console)
    {
        BaseStackConfigDirectory = Path.Join(ConfigUtilities.BaseConfigDirectoryPath, "stack");
    }

    private void EnsureStackConfigDirectoryExists()
    {
        Task.WaitAll(MdsSdk.DirectoryInitialize());
        DirectoryTools.EnsurePathsExists(
            BaseStackConfigDirectory,
            Path.Join(BaseStackConfigDirectory, "logs")
        );
    }

    private T? SafeLoadFile<T>(string path)
        where T : class
    {
        EnsureStackConfigDirectoryExists();

        var settings = File.Exists(path)
            ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path))
            : null;

        return settings;
    }

    private MdsStackSettings? GetStackSettings()
    {
        EnsureStackConfigDirectoryExists();

        var settingsFilePath = Path.Join(BaseStackConfigDirectory, Constants.SettingsFileName);
        var settings = SafeLoadFile<MdsStackSettings>(settingsFilePath);
        return !string.IsNullOrEmpty(settings?.SourceDirectory) ? settings : null;
    }

    private MdsStackConfiguration? GetStackConfiguration()
    {
        EnsureStackConfigDirectoryExists();

        var configFilePath = Path.Join(BaseStackConfigDirectory, Constants.ConfigurationFileName);
        var config = SafeLoadFile<MdsStackConfiguration>(configFilePath);
        return !string.IsNullOrEmpty(config?.Identity) ? config : null;
    }

    private void UpdateStackConfiguration(
        MdsStackConfiguration configuration,
        string key,
        string value
    )
    {
        foreach (var propertyInfo in typeof(MdsStackConfiguration).GetProperties())
        {
            if (
                (
                    from attribute in propertyInfo.GetCustomAttributes(true)
                    where typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType()
                    select (ConfigElementDisplaySettingsAttribute)attribute
                ).All(castAttribute => castAttribute.Key != key)
            )
                continue;
            propertyInfo.SetValue(configuration, value);
            return;
        }

        throw new Exception($"{key} not found");
    }

    private Table GetDisplayTable(MdsStackConfiguration? configuration)
    {
        var table = new Table();
        table.AddColumns("Service", "Image Source");
        table.Border(TableBorder.Ascii);
        table.ShowHeaders = true;

        foreach (var propertyInfo in typeof(MdsStackConfiguration).GetProperties())
        {
            foreach (var attribute in propertyInfo.GetCustomAttributes(true))
            {
                if (typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType())
                {
                    var castAttribute = (ConfigElementDisplaySettingsAttribute)attribute;
                    var label =
                        castAttribute.DisplayPrompt
                        ?? $"[red]ERROR[/]: DisplayPrompt not found for key [olive]{castAttribute.Key}[/]";
                    var value =
                        (string?)propertyInfo.GetValue(configuration) != null
                            ? (string)propertyInfo.GetValue(configuration)!
                            : $"[yellow]WARNING[/]: Missing validator for [olive]{castAttribute.Key}[/]";

                    table.AddRow(label, value);
                }
            }
        }

        return table;
    }

    private bool ValidateSettings(MdsStackSettings? settings)
    {
        if (settings != null)
            return true;

        this.AnsiConsole.MarkupLine(
            "Cannot perform operation since configuration not present. Please run [olive]mds stack init[/] first."
        );
        return false;
    }

    private static string ToSpectreConsoleSafeString(string message)
    {
        return message.Replace("[", "[[").Replace("]", "]]");
    }

    [Command("init")]
    public void Init()
    {
        var validators = MdsStackSettings.GetValidators();

        EnsureStackConfigDirectoryExists();
        var settings = GetStackSettings() ?? new MdsStackSettings();

        foreach (var propertyInfo in typeof(MdsStackSettings).GetProperties())
        {
            foreach (var attribute in propertyInfo.GetCustomAttributes(true))
            {
                if (typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType())
                {
                    var castAttribute = (ConfigElementDisplaySettingsAttribute)attribute;
                    var dynamicPrompt = new TextPrompt<string>(
                        castAttribute.QueryPrompt
                            ?? $"[red]ERROR[/]: QueryPrompt not found for key [olive]{castAttribute.Key}[/]"
                    ).AllowEmpty();

                    if (castAttribute.HideValue)
                    {
                        dynamicPrompt.Secret();
                    }

                    if ((string?)propertyInfo.GetValue(settings) != null)
                    {
                        dynamicPrompt
                            .ShowDefaultValue()
                            .DefaultValue((string)propertyInfo.GetValue(settings)!);
                    }

                    if (validators.TryGetValue(castAttribute.Key, out var validator))
                    {
                        dynamicPrompt.Validator = value =>
                            validator(value)
                                ? ValidationResult.Success()
                                : ValidationResult.Error();
                    }
                    else
                    {
                        this.AnsiConsole.MarkupLine(
                            $"[yellow]WARNING[/]: Missing validator for [olive]{castAttribute.Key}[/]"
                        );
                    }

                    var answer = this.AnsiConsole.Prompt(dynamicPrompt);
                    propertyInfo.SetValue(settings, answer);
                }
            }
        }

        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, Constants.SettingsFileName),
            JsonConvert.SerializeObject(settings, Formatting.Indented)
        );
        this.AnsiConsole.WriteLine("Settings updated!");
    }

    [Command("config")]
    public void Config()
    {
        var configuration = GetStackConfiguration() ?? new MdsStackConfiguration();

        var table = GetDisplayTable(configuration);

        string selection;
        var services = new List<string>
        {
            Constants.StackConfiguration.Keys.Identity,
            Constants.StackConfiguration.Keys.NotificationService,
            Constants.StackConfiguration.Keys.QueueService,
            Constants.StackConfiguration.Keys.FileService,
            Constants.StackConfiguration.Keys.ServerlessFunctions,
            Constants.StackConfiguration.Keys.StateMachine,
        };
        var exitOptions = new List<string> { "[[Save]]", "[[Cancel]]" };

        do
        {
            this.AnsiConsole.Write(table);
            var options = new List<string>().Concat(services).Concat(exitOptions);
            selection = this.AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(
                        "Select a service to adjust. Choose [[Save]] to update the configuration and [[Cancel]] to discard changes."
                    )
                    .AddChoices(options)
            );

            if (services.Contains(selection))
            {
                var source = this.AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title(
                            $"Which container mode would you like to use for {selection}? Local dev indicates the service will not run in a docker container."
                        )
                        .AddChoices(
                            new[]
                            {
                                Constants.StackConfiguration.Values.Stable,
                                Constants.StackConfiguration.Values.Latest,
                                Constants.StackConfiguration.Values.Local,
                                Constants.StackConfiguration.Values.LocalDev,
                            }
                        )
                );
                UpdateStackConfiguration(configuration, selection, source);
                table = GetDisplayTable(configuration);
            }
            this.AnsiConsole.Cursor.Move(CursorDirection.Up, services.Count + 4); // + 4 for header and three borders
        } while (!exitOptions.Contains(selection));

        if (selection == "[[Save]]")
        {
            File.WriteAllText(
                Path.Join(BaseStackConfigDirectory, Constants.ConfigurationFileName),
                JsonConvert.SerializeObject(configuration, Formatting.Indented)
            );
            table = GetDisplayTable(configuration);
            this.AnsiConsole.Write(table);
            this.AnsiConsole.WriteLine("Saved image source settings successfully!");
        }
        else
        {
            configuration = GetStackConfiguration() ?? new MdsStackConfiguration();
            table = GetDisplayTable(configuration);
            this.AnsiConsole.Write(table);
            this.AnsiConsole.WriteLine("Reverted image source settings successfully!");
        }
    }

    [Command("build")]
    public void Build([Option("env")] string? environment = null)
    {
        var settings = GetStackSettings();
        var config = GetStackConfiguration() ?? new MdsStackConfiguration();

        if (!ValidateSettings(settings))
        {
            return;
        }
        this.AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Ascii)
            .Start(
                "Building Containers",
                context =>
                {
                    void BuilderStatusUpdateHandler(object? _, string message)
                    {
                        context.Status(ToSpectreConsoleSafeString(message));
                    }

                    void BuilderMilestoneAchieved(object? _, string message)
                    {
                        this.AnsiConsole.MarkupLine(ToSpectreConsoleSafeString(message));
                    }

                    try
                    {
                        var manager = new StackServiceConfigManager(BaseStackConfigDirectory);
                        manager.OnMilestoneAchieved += BuilderMilestoneAchieved;
                        manager.OnStatusUpdate += BuilderStatusUpdateHandler;
                        manager.Configure(settings, config);
                    }
                    catch (Exception)
                    {
                        this.AnsiConsole.Markup(
                            "[red]One or more child processes failed. See logs[/]"
                        );
                        this.AnsiConsole.Markup(Path.Join(BaseStackConfigDirectory, "logs"));
                    }
                }
            );
    }

    [Command("up")]
    public void Up()
    {
        var images = new List<string>();
        using var imagesProcess = new ChildProcess(
            "docker images --format {{.Repository}}:{{.Tag}}",
            BaseStackConfigDirectory
        );
        imagesProcess.OnDataOutput += (_, imageKey) => images.Add(imageKey);
        imagesProcess.Start();
        imagesProcess.WaitForExit();

        var shouldBuild = false;

        var config = GetStackConfiguration() ?? new MdsStackConfiguration();
        foreach (var propertyInfo in typeof(MdsStackConfiguration).GetProperties())
        {
            foreach (var attribute in propertyInfo.GetCustomAttributes(true))
            {
                if (typeof(ConfigElementDockerSettingsAttribute) != attribute.GetType())
                    continue;

                var castAttribute = (ConfigElementDockerSettingsAttribute)attribute;
                var value =
                    (string?)propertyInfo.GetValue(config) != null
                        ? (string)propertyInfo.GetValue(config)!
                        : Constants.StackConfiguration.Values.Default;

                if (value == Constants.StackConfiguration.Values.Local)
                {
                    var imageExists = images.Any(
                        image => image == $"local/{castAttribute.DockerImageName}:latest"
                    );

                    shouldBuild = shouldBuild || !imageExists;
                }
            }
        }

        if (shouldBuild)
        {
            Build();
        }

        // TODO: Pipe through compose messages
        using var composeProcess = new ChildProcess(
            $"docker compose -p mds-stack up -d",
            BaseStackConfigDirectory,
            Path.Join(BaseStackConfigDirectory, "logs", "stack-init.log")
        );

        composeProcess.Start();
        this.AnsiConsole.WriteLine("Bringing stack up...");
        this.AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Ascii)
            .Start(
                "Bringing stack up...",
                context =>
                {
                    composeProcess.OnDataOutput += (_, message) =>
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            context.Status(message);
                        }
                    };
                    composeProcess.WaitForExit();
                }
            );

        if (composeProcess.ExitCode == 0)
        {
            this.AnsiConsole.WriteLine("Stack processes initializing.");
        }
        else
        {
            this.AnsiConsole.Write(
                File.ReadAllText(Path.Join(BaseStackConfigDirectory, "logs", "stack-init.log"))
            );
        }
    }

    [Command("down")]
    public void Down()
    {
        // TODO: Pipe through compose messages
        using var composeProcess = new ChildProcess(
            $"docker compose -p mds-stack down -v",
            BaseStackConfigDirectory,
            Path.Join(BaseStackConfigDirectory, "logs", "stack-down.log")
        );

        composeProcess.Start();
        this.AnsiConsole.WriteLine("Bringing stack down...");
        composeProcess.WaitForExit();

        if (composeProcess.ExitCode == 0)
        {
            this.AnsiConsole.WriteLine("Stack processes halted.");
        }
        else
        {
            this.AnsiConsole.Write(
                File.ReadAllText(Path.Join(BaseStackConfigDirectory, "logs", "stack-down.log"))
            );
        }
    }
}
