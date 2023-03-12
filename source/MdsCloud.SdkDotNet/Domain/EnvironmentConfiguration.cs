using MdsCloud.SdkDotNet.Attributes;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Domain;

public class EnvironmentConfiguration
{
    [JsonProperty("account")]
    [ConfigElementDisplaySettings(Key = "account", Prompt = "Account")]
    public string? AccountId { get; set; }

    [JsonProperty("userId")]
    [ConfigElementDisplaySettings(Key = "userId", Prompt = "User Id")]
    public string? UserId { get; set; }

    [JsonProperty("password")]
    [ConfigElementDisplaySettings(Key = "password", Prompt = "Password", HideValue = true)]
    public string? Password { get; set; }

    [JsonProperty("identityUrl")]
    [ConfigElementDisplaySettings(Key = "identityUrl", Prompt = "MdsCloud.Identity Service Url")]
    public string? IdentityUrl { get; set; }

    [JsonProperty("nsUrl")]
    [ConfigElementDisplaySettings(Key = "nsUrl", Prompt = "Notification Service Url")]
    public string? NotificationServiceUrl { get; set; }

    [JsonProperty("qsUrl")]
    [ConfigElementDisplaySettings(Key = "qsUrl", Prompt = "Queue Service Url")]
    public string? QueueServiceUrl { get; set; }

    [JsonProperty("fsUrl")]
    [ConfigElementDisplaySettings(Key = "fsUrl", Prompt = "File Service Url")]
    public string? FileServiceUrl { get; set; }

    [JsonProperty("sfUrl")]
    [ConfigElementDisplaySettings(Key = "sfUrl", Prompt = "Serverless Functions Service Url")]
    public string? ServerlessFunctionsServiceUrl { get; set; }

    [JsonProperty("smUrl")]
    [ConfigElementDisplaySettings(Key = "smUrl", Prompt = "State Machine Service Url")]
    public string? StateMachineServiceUrl { get; set; }

    [JsonProperty("allowSelfSignCert")]
    [ConfigElementDisplaySettings(
        Key = "allowSelfSignCert",
        Prompt = "Allow self-signed Certificates"
    )]
    public bool? AllowSelfSignCertificate { get; set; }
}
