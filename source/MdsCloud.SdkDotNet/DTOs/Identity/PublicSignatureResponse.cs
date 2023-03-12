using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.DTOs.Identity;

public class PublicSignatureResponse
{
    [JsonProperty("signature")]
    public string? Signature { get; set; }
}
