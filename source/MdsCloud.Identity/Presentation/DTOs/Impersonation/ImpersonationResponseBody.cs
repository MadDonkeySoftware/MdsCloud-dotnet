using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.Presentation.DTOs.Impersonation;

public class ImpersonationResponseBody
{
    [Required]
    [SwaggerSchema("The user specific JWT to be supplied for authentication")]
    public string Token { get; set; }
}
