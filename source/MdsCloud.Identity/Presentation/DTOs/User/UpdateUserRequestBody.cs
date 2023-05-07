#pragma warning disable CS8618
namespace MdsCloud.Identity.Presentation.DTOs.User;

public class UpdateUserRequestBody
{
    // [SwaggerSchema("The account name to authenticate against")]
    public string? OldPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? FriendlyName { get; set; }

    public string? Email { get; set; }
}
