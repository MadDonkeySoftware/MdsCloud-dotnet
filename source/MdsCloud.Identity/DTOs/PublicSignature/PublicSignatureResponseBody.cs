using Microsoft.Build.Framework;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.DTOs.PublicSignature;

public class PublicSignatureResponseBody
{
    [Required]
    [SwaggerSchema("")]
    public string Signature { get; set; }
}
