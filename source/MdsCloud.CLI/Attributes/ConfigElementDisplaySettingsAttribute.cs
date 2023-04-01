#pragma warning disable CS8618
namespace MdsCloud.CLI.Attributes;

public class ConfigElementDisplaySettingsAttribute : Attribute
{
    public string? DisplayPrompt { get; set; }
    public string? QueryPrompt { get; set; }
    public int DisplayOrder { get; set; }
    public string Key { get; set; }
    public bool HideValue { get; set; }
}
