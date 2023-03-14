using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.DTOs.Identity;

public class RegisterRequest
{
    [JsonProperty("userId")]
    public string? UserId { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }

    [JsonProperty("friendlyName")]
    public string? FriendlyName { get; set; }

    [JsonProperty("accountName")]
    public string? AccountName { get; set; }
}
