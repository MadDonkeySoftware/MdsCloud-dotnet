using MadDonkeySoftware.SystemWrappers.Runtime;
using MdsCloud.CLI.Domain;
using Newtonsoft.Json;

namespace MdsCloud.CLI.Utilities;

[Obsolete] // TODO: Moved to SdkDotNet
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

    public MdsCliConfiguration GetConfig(string environment)
    {
        var envFilePath = Path.Join(_baseConfigDirectoryPath, $"{environment}.json");
        var fileContent = File.ReadAllText(envFilePath);
        var envConfig = JsonConvert.DeserializeObject<MdsCliConfiguration>(fileContent);

        return envConfig ?? new MdsCliConfiguration();
    }

    public void SaveConfig(string environment, MdsCliConfiguration config)
    {
        var envFilePath = Path.Join(_baseConfigDirectoryPath, $"{environment}.json");
        File.WriteAllText(envFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}
