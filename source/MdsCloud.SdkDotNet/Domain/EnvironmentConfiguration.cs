using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Domain;

public class EnvironmentConfiguration
{
    [JsonProperty("account")]
    public virtual string? AccountId { get; set; }

    [JsonProperty("userId")]
    public virtual string? UserId { get; set; }

    [JsonProperty("password")]
    public virtual string? Password { get; set; }

    [JsonProperty("identityUrl")]
    public virtual string? IdentityUrl { get; set; }

    [JsonProperty("nsUrl")]
    public virtual string? NotificationServiceUrl { get; set; }

    [JsonProperty("qsUrl")]
    public virtual string? QueueServiceUrl { get; set; }

    [JsonProperty("fsUrl")]
    public virtual string? FileServiceUrl { get; set; }

    [JsonProperty("sfUrl")]
    public virtual string? ServerlessFunctionsServiceUrl { get; set; }

    [JsonProperty("smUrl")]
    public virtual string? StateMachineServiceUrl { get; set; }

    [JsonProperty("allowSelfSignCert")]
    public virtual bool? AllowSelfSignCertificate { get; set; }
}
