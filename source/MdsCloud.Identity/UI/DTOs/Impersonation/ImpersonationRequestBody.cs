using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.UI.DTOs.Impersonation;

public class ImpersonationRequestBody
{
    [Required]
    [SwaggerSchema("The account to impersonate against")]
    public string AccountId { get; set; }

    [SwaggerSchema("The user name to impersonate")]
    public string? UserId { get; set; }
}
