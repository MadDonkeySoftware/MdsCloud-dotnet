using Microsoft.Build.Framework;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.DTOs.Authentication;

public class AuthenticationRequestBody
{
    [Required]
    [SwaggerSchema("The account name to authenticate against")]
    public string AccountId { get; set; }

    [Required]
    [SwaggerSchema("The user name to authenticate with")]
    public string UserId { get; set; }

    [Required]
    [SwaggerSchema("The password to authenticate with")]
    public string Password { get; set; }
}
