using MdsCloud.CLI.Attributes;
using Newtonsoft.Json;

namespace MdsCloud.CLI.Domain;

public class MdsStackSettings
{
    [JsonProperty(Constants.StackSettingsKeys.SourceDirectory)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackSettingsKeys.SourceDirectory,
        QueryPrompt = $"What folder does the \"{Constants.SolutionFileName}\" file reside in?",
        DisplayPrompt = "Source Directory"
    )]
    public string? SourceDirectory { get; set; } = string.Empty;

    [JsonProperty(Constants.StackSettingsKeys.DefaultAdminPassword)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackSettingsKeys.DefaultAdminPassword,
        QueryPrompt = "What would you like to use for your local stack administrator password?",
        DisplayPrompt = "Default Admin Password",
        HideValue = true
    )]
    public string? DefaultAdminPassword { get; set; } = string.Empty;

    internal static Dictionary<string, Func<string, bool>> GetValidators()
    {
        return new Dictionary<string, Func<string, bool>>
        {
            { Constants.StackSettingsKeys.DefaultAdminPassword, _ => true },
            {
                Constants.StackSettingsKeys.SourceDirectory,
                sourceDirPath =>
                    string.IsNullOrEmpty(sourceDirPath)
                    || File.Exists(Path.Join(sourceDirPath, Constants.SolutionFileName))
            }
        };
    }
}
