namespace MdsCloud.Identity.Settings;

public interface ISettings
{
    string? this[string key] { get; }
}
