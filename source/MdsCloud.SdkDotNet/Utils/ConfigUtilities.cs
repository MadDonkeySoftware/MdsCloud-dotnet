using MadDonkeySoftware.SystemWrappers.Runtime;
using MdsCloud.SdkDotNet.Domain;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Utils;

public class ConfigUtilities : IConfigUtilities
{
    public string BaseConfigDirectoryPath { get; }

    public ConfigUtilities(IEnvironment environment)
    {
        BaseConfigDirectoryPath = Path.Join(
            environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mds"
        );
    }

    public Task EnsureBaseConfigDirectoryExists()
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(BaseConfigDirectoryPath))
            {
                if (OperatingSystem.IsWindows())
                {
                    Directory.CreateDirectory(BaseConfigDirectoryPath);
                }
                else
                {
                    Directory.CreateDirectory(
                        BaseConfigDirectoryPath,
                        UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite
                    );
                }
            }
        });
    }

    public string GetDefaultEnvironment()
    {
        var envFilePath = Path.Join(BaseConfigDirectoryPath, "selectedEnv");
        return File.Exists(envFilePath) ? File.ReadAllText(envFilePath) : "default";
    }

    public EnvironmentConfiguration GetConfig(string environment)
    {
        var envFilePath = Path.Join(BaseConfigDirectoryPath, $"{environment}.json");
        var fileContent = File.ReadAllText(envFilePath);
        var envConfig = JsonConvert.DeserializeObject<EnvironmentConfiguration>(fileContent);

        return envConfig ?? new EnvironmentConfiguration();
    }

    public void SaveConfig(string environment, EnvironmentConfiguration config)
    {
        var envFilePath = Path.Join(BaseConfigDirectoryPath, $"{environment}.json");
        File.WriteAllText(envFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}
