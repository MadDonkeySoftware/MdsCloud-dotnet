using MdsCloud.SdkDotNet.Domain;

namespace MdsCloud.SdkDotNet.Utils;

public interface IConfigUtilities
{
    string GetDefaultEnvironment();
    EnvironmentConfiguration GetConfig(string environment);
    void SaveConfig(string environment, EnvironmentConfiguration config);
}
