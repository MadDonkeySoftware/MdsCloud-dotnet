using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.DTOs.Identity;

public class UpdateUserRequest
{
    [JsonProperty("newPassword")]
    public string? NewPassword { get; set; }

    [JsonProperty("oldPassword")]
    public string? OldPassword { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("friendlyName")]
    public string? FriendlyName { get; set; }

    public bool HasValue()
    {
        return NewPassword != null || OldPassword != null || Email != null || FriendlyName != null;
    }
}
