using System.ComponentModel.DataAnnotations;
using MdsCloud.Identity.Business.DTOs;
using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.UI.DTOs.Authentication;

public class AuthenticationRequestBody : GenerateUserTokenArgs
{
    [Required]
    [SwaggerSchema("The account name to authenticate against")]
    public override string AccountId { get; set; }

    [Required]
    [SwaggerSchema("The user name to authenticate with")]
    public override string UserId { get; set; }

    [Required]
    [SwaggerSchema("The password to authenticate with")]
    public override string Password { get; set; }
}
