#pragma warning disable CS8618
namespace MdsCloud.SdkDotNet.Attributes;

public class ConfigElementDisplaySettingsAttribute : Attribute
{
    public string Prompt { get; set; }
    public string Key { get; set; }
    public bool HideValue { get; set; }
}
