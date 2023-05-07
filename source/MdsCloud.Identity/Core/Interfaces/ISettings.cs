namespace MdsCloud.Identity.Core.Interfaces;

public interface ISettings
{
    string? this[string key] { get; }
}
