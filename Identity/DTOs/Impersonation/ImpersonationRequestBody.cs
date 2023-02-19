using Microsoft.Build.Framework;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace Identity.DTOs.Impersonation;

public class ImpersonationRequestBody
{
    [Required]
    [SwaggerSchema("The account to impersonate against")]
    public string AccountId { get; set; }

    [SwaggerSchema("The user name to impersonate")]
    public string? UserId { get; set; }
}
