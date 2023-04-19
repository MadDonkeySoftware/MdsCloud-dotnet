#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using MdsCloud.Identity.Business.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.UI.DTOs.Registration;

public class RegistrationRequestBody : AccountRegistrationArgs
{
    [Required]
    [SwaggerSchema("The friendly name of the account")]
    public override string AccountName { get; set; }

    [Required]
    [SwaggerSchema("The email account to associate with the accounts primary user")]
    public override string Email { get; set; }

    [Required]
    [SwaggerSchema("The friendly name to associate with the accounts primary user")]
    public override string FriendlyName { get; set; }

    [Required]
    [SwaggerSchema("The password to associate with the accounts primary user")]
    public override string Password { get; set; }

    [Required]
    [SwaggerSchema("The login/user name to associate with the accounts primary user")]
    public override string UserId { get; set; }
}
