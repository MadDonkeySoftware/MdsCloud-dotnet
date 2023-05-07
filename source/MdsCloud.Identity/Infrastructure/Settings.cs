using MdsCloud.Identity.Core.Interfaces;

namespace MdsCloud.Identity.Infrastructure;

public class Settings : ISettings
{
    // NOTE: I know this seems a bit needless but this allows a way to easily mock
    // settings that come from the configs while testing w/o breaking the way the
    // app is constructed.

    private IConfiguration? Configuration { get; }

    public Settings() { }

    public Settings(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public string? this[string key] => Configuration?[key];
}
