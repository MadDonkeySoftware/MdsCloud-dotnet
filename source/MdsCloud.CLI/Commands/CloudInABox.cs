using CommandDotNet;
using MdsCloud.SdkDotNet;
using MdsCloud.SdkDotNet.Utils;
using Newtonsoft.Json;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

internal class StackSettings
{
    [JsonProperty("sourceDirectory")]
    public string? SourceDirectory { get; set; } = string.Empty;
}

public class CloudInABox : BaseMdsCommand
{
    private const string SolutionFileName = "MdsCloud.All.sln";
    private const string SettingsFileName = "settings.json";

    private string BaseStackConfigDirectory { get; }

    public CloudInABox(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console)
    {
        BaseStackConfigDirectory = Path.Join(ConfigUtilities.BaseConfigDirectoryPath, "stack");
    }

    private void EnsureStackConfigDirectoryExists()
    {
        if (!Directory.Exists(BaseStackConfigDirectory))
        {
            Task.WaitAll(MdsSdk.DirectoryInitialize());
            try
            {
                var info = Directory.CreateDirectory(BaseStackConfigDirectory);
                Console.WriteLine(info.Exists);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    [Command("config")]
    public void Configure([Option("env")] string? environment = null)
    {
        EnsureStackConfigDirectoryExists();

        StackSettings? settings = null;
        var settingsFilePath = Path.Join(BaseStackConfigDirectory, SettingsFileName);
        if (File.Exists(settingsFilePath))
        {
            settings = JsonConvert.DeserializeObject<StackSettings>(
                File.ReadAllText(settingsFilePath)
            );
        }

        var sourcePathPrompt = new TextPrompt<string>(
            $"What folder does the \"{SolutionFileName}\" file reside in?"
        );

        var answer = this.AnsiConsole.Prompt(
            settings?.SourceDirectory != null
                ? sourcePathPrompt.DefaultValue(settings.SourceDirectory).ShowDefaultValue()
                : sourcePathPrompt
        );

        settings ??= new StackSettings();

        if (File.Exists(Path.Join(answer, SolutionFileName)))
        {
            settings.SourceDirectory = answer;
            File.WriteAllText(
                Path.Join(BaseStackConfigDirectory, SettingsFileName),
                JsonConvert.SerializeObject(settings, Formatting.Indented)
            );
            this.AnsiConsole.WriteLine("Settings updated!");
        }
        else
        {
            this.AnsiConsole.WriteLine(
                $"Could not find solution file ({SolutionFileName}) at the location: {answer}"
            );
        }
    }
}
