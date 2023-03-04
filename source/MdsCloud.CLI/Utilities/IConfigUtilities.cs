using MdsCloud.CLI.Domain;

namespace MdsCloud.CLI.Utilities;

public interface IConfigUtilities
{
    string GetDefaultEnvironment();
    MdsCliConfiguration GetConfig(string environment);
    void SaveConfig(string environment, MdsCliConfiguration config);
}
