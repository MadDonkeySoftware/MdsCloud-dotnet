using MdsCloud.CLI.Attributes;
using Newtonsoft.Json;

namespace MdsCloud.CLI.Domain;

public class MdsStackConfiguration
{
    [JsonProperty(Constants.StackConfiguration.Keys.Identity)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.Identity,
        DisplayPrompt = "Identity"
    )]
    [ConfigElementDockerSettings(DockerImageName = Constants.DockerImageNames.Identity)]
    public virtual string Identity { get; set; } = Constants.StackConfiguration.Values.Default;

    [JsonProperty(Constants.StackConfiguration.Keys.NotificationService)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.NotificationService,
        DisplayPrompt = "Notification Service"
    )]
    public virtual string NotificationService { get; set; } =
        Constants.StackConfiguration.Values.Default;

    [JsonProperty(Constants.StackConfiguration.Keys.QueueService)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.QueueService,
        DisplayPrompt = "Queue Service"
    )]
    public virtual string QueueService { get; set; } = Constants.StackConfiguration.Values.Default;

    [JsonProperty(Constants.StackConfiguration.Keys.FileService)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.FileService,
        DisplayPrompt = "File Service"
    )]
    public virtual string FileService { get; set; } = Constants.StackConfiguration.Values.Default;

    [JsonProperty(Constants.StackConfiguration.Keys.ServerlessFunctions)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.ServerlessFunctions,
        DisplayPrompt = "Serverless Functions Service"
    )]
    public virtual string ServerlessFunctionsService { get; set; } =
        Constants.StackConfiguration.Values.Default;

    [JsonProperty(Constants.StackConfiguration.Keys.StateMachine)]
    [ConfigElementDisplaySettings(
        Key = Constants.StackConfiguration.Keys.StateMachine,
        DisplayPrompt = "State Machine Service"
    )]
    public virtual string StateMachineService { get; set; } =
        Constants.StackConfiguration.Values.Default;

    public virtual string GetStackConfigurationValue(string service)
    {
        // If reflection gets too bad on performance this can be updated
        // to be a lookup against a cache that changes when values above
        // are updated.
        foreach (var propertyInfo in typeof(MdsStackConfiguration).GetProperties())
        {
            if (
                (
                    from attribute in propertyInfo.GetCustomAttributes(true)
                    where typeof(ConfigElementDisplaySettingsAttribute) == attribute.GetType()
                    select (ConfigElementDisplaySettingsAttribute)attribute
                ).All(attr => attr.Key != service)
            )
            {
                continue;
            }

            return (string?)propertyInfo.GetValue(this)
                ?? Constants.StackConfiguration.Values.Default;
        }

        return Constants.StackConfiguration.Values.Default;
    }
}
