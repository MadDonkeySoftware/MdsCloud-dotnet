using MadDonkeySoftware.SystemWrappers.Runtime;
using MdsCloud.SdkDotNet.Domain;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Utils;

public class ConfigUtilities : IConfigUtilities
{
    private readonly string _baseConfigDirectoryPath;

    public ConfigUtilities(IEnvironment environment)
    {
        _baseConfigDirectoryPath = Path.Join(
            environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mds"
        );
    }

    public string GetDefaultEnvironment()
    {
        var envFilePath = Path.Join(_baseConfigDirectoryPath, "selectedEnv");
        return File.Exists(envFilePath) ? File.ReadAllText(envFilePath) : "default";
    }

    public EnvironmentConfiguration GetConfig(string environment)
    {
        var envFilePath = Path.Join(_baseConfigDirectoryPath, $"{environment}.json");
        var fileContent = File.ReadAllText(envFilePath);
        var envConfig = JsonConvert.DeserializeObject<EnvironmentConfiguration>(fileContent);

        return envConfig ?? new EnvironmentConfiguration();
    }

    public void SaveConfig(string environment, EnvironmentConfiguration config)
    {
        var envFilePath = Path.Join(_baseConfigDirectoryPath, $"{environment}.json");
        File.WriteAllText(envFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}
