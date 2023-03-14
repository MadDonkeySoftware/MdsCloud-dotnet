using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.DTOs.Identity;

public class RegistrationResponse
{
    [JsonProperty("accountId")]
    public string? AccountId { get; set; }
}
