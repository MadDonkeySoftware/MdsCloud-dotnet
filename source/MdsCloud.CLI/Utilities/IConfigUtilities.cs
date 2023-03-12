using MdsCloud.CLI.Domain;

namespace MdsCloud.CLI.Utilities;

[Obsolete] // TODO: Moved to SdkDotNet
public interface IConfigUtilities
{
    string GetDefaultEnvironment();
    MdsCliConfiguration GetConfig(string environment);
    void SaveConfig(string environment, MdsCliConfiguration config);
}
