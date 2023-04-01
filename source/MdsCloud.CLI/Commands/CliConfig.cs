using CommandDotNet;
using MdsCloud.CLI.Attributes;
using MdsCloud.CLI.Domain;
using MdsCloud.SdkDotNet.Utils;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

public class CliConfig : BaseMdsCommand
{
    public CliConfig(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console) { }

    private void ShowConfigKeys()
    {
        var keys = (
            from propertyInfo in typeof(MdsCliConfiguration).GetProperties()
            from attribute in propertyInfo.GetCustomAttributes(true)
            where typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType()
            select ((ConfigElementDisplaySettingsAttribute)attribute).Key
        ).ToList();

        var slug = string.Join(", ", keys);
        this.AnsiConsole.WriteLine($"Known Keys: {slug}");
    }

    [Command("inspect")]
    public void Inspect(
        [Operand(
            Description = "The element to inspect from the environment. Use \"?\" to get a list of available elements"
        )]
            string element = "all",
        [Option("env")] string? environment = null
    )
    {
        if (element == "?")
        {
            ShowConfigKeys();
        }
        else if (element == "all")
        {
            var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
            var config = this.ConfigUtilities.GetConfig(env);

            var table = new Table();
            table.AddColumns("", "");
            table.Border(TableBorder.None);
            table.ShowHeaders = false;

            foreach (var propertyInfo in typeof(MdsCliConfiguration).GetProperties())
            {
                var displayPrompt = "Unknown";
                var displayValue = "";
                foreach (var attribute in propertyInfo.GetCustomAttributes(true))
                {
                    if (typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType())
                    {
                        var castAttribute = (ConfigElementDisplaySettingsAttribute)attribute;
                        displayValue = castAttribute.HideValue
                            ? "***"
                            : propertyInfo.GetValue(config)?.ToString() ?? "";
                        displayPrompt = castAttribute.DisplayPrompt;
                    }
                }
                table.AddRow(displayPrompt, displayValue);
            }

            this.AnsiConsole.Write(table);
        }
        else
        {
            var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
            var config = this.ConfigUtilities.GetConfig(env);

            var displayValue = "";
            foreach (var propertyInfo in typeof(MdsCliConfiguration).GetProperties())
            {
                foreach (var attribute in propertyInfo.GetCustomAttributes(true))
                {
                    if (typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType())
                    {
                        var castAttribute = (ConfigElementDisplaySettingsAttribute)attribute;
                        if (castAttribute.Key == element)
                        {
                            displayValue = propertyInfo.GetValue(config)?.ToString() ?? "";
                        }
                    }
                }
            }

            if (displayValue == string.Empty)
            {
                this.AnsiConsole.WriteLine($"Could not find value for key: {element}");
                ShowConfigKeys();
            }
            else
            {
                this.AnsiConsole.WriteLine(displayValue);
            }
        }
    }

    [Command("wizard")]
    public void Wizard([Option("env")] string? environment = null)
    {
        var env = environment ?? ConfigUtilities.GetDefaultEnvironment();
        var config = ConfigUtilities.GetConfig(env);

        var orderedProps = typeof(MdsCliConfiguration)
            .GetProperties()
            .OrderBy(
                prop =>
                    prop.GetCustomAttributes(true)
                        .OfType<ConfigElementDisplaySettingsAttribute>()
                        .First()
                        .DisplayOrder
            )
            .Select(p => p);

        var updated = false;

        foreach (var propertyInfo in orderedProps)
        {
            foreach (
                var attribute in propertyInfo
                    .GetCustomAttributes(true)
                    .OfType<ConfigElementDisplaySettingsAttribute>()
            )
            {
                if (propertyInfo.PropertyType == typeof(bool?))
                {
                    var previous = (bool?)propertyInfo.GetValue(config);
                    var previousConverted = previous.HasValue && previous.Value ? "Yes" : "No";
                    var promptTitle =
                        $"{attribute.QueryPrompt ?? "MISSING: QueryPrompt attribute value"} [green](Previously: {previousConverted})[/]";

                    var prompt = new SelectionPrompt<string>()
                        .Title(promptTitle)
                        .AddChoices("No", "Yes");

                    var newValue = AnsiConsole.Prompt(prompt);

                    propertyInfo.SetValue(config, newValue == "Yes");

                    if (previousConverted != newValue)
                    {
                        updated = true;
                    }

                    this.AnsiConsole.MarkupLine($"{promptTitle}: {newValue}");
                }
                else
                {
                    var previousValue = (string?)propertyInfo.GetValue(config);
                    var prompt = new TextPrompt<string?>(
                        attribute.QueryPrompt ?? "MISSING: QueryPrompt attribute value"
                    )
                        .DefaultValue(previousValue)
                        .ShowDefaultValue(true);

                    prompt.IsSecret = attribute.HideValue;
                    var newValue = AnsiConsole.Prompt(prompt);

                    if (previousValue != newValue)
                    {
                        updated = true;
                    }

                    propertyInfo.SetValue(config, newValue);
                }
            }
        }

        if (updated)
        {
            this.ConfigUtilities.SaveConfig(env, config);
            this.AnsiConsole.WriteLine("Changes saved successfully.");
        }
        else
        {
            this.AnsiConsole.WriteLine("No changes detected.");
        }
    }

    [Command("write")]
    public void Write(
        [Operand(Description = "The element to write in the environment")] string key,
        [Operand(Description = "The new value of the element")] string value,
        [Option("env")] string? environment = null
    )
    {
        var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
        var config = this.ConfigUtilities.GetConfig(env);
        var updated = false;

        foreach (var propertyInfo in typeof(MdsCliConfiguration).GetProperties())
        {
            foreach (var attribute in propertyInfo.GetCustomAttributes(true))
            {
                if (typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType())
                {
                    var castAttribute = (ConfigElementDisplaySettingsAttribute)attribute;
                    if (castAttribute.Key == key)
                    {
                        propertyInfo.SetValue(config, value);
                        updated = true;
                    }
                }
            }
        }

        if (updated)
        {
            this.ConfigUtilities.SaveConfig(env, config);
        }
        else
        {
            this.AnsiConsole.WriteLine(
                "Could not update config. Please verify your config key and try again."
            );
            ShowConfigKeys();
        }
    }
}
