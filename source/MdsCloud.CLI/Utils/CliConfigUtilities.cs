using MdsCloud.CLI.Domain;
using MdsCloud.SdkDotNet.Domain;
using MdsCloud.SdkDotNet.Utils;

namespace MdsCloud.CLI.Utils;

public class CliConfigUtilities : IConfigUtilities
{
    private readonly ConfigUtilities _configUtilities;

    public CliConfigUtilities(ConfigUtilities sdkConfigUtilities)
    {
        _configUtilities = sdkConfigUtilities;
    }

    public string BaseConfigDirectoryPath => _configUtilities.BaseConfigDirectoryPath;

    public Task EnsureBaseConfigDirectoryExists()
    {
        return _configUtilities.EnsureBaseConfigDirectoryExists();
    }

    public string GetDefaultEnvironment()
    {
        return _configUtilities.GetDefaultEnvironment();
    }

    public EnvironmentConfiguration GetConfig(string environment)
    {
        return new MdsCliConfiguration(_configUtilities.GetConfig(environment));
    }

    public void SaveConfig(string environment, EnvironmentConfiguration config)
    {
        _configUtilities.SaveConfig(environment, config);
    }
}
