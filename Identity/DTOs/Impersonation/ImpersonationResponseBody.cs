using Microsoft.Build.Framework;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace Identity.DTOs.Impersonation;

public class ImpersonationResponseBody
{
    [Required]
    [SwaggerSchema("The user specific JWT to be supplied for authentication")]
    public string Token { get; set; }
}
