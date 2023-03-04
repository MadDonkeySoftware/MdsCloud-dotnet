#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.DTOs.Registration;

public class RegistrationRequestBody
{
    [Required]
    [SwaggerSchema("The friendly name of the account")]
    public string AccountName { get; set; }

    [Required]
    [SwaggerSchema("The email account to associate with the accounts primary user")]
    public string Email { get; set; }

    [Required]
    [SwaggerSchema("The friendly name to associate with the accounts primary user")]
    public string FriendlyName { get; set; }

    [Required]
    [SwaggerSchema("The password to associate with the accounts primary user")]
    public string Password { get; set; }

    [Required]
    [SwaggerSchema("The login/user name to associate with the accounts primary user")]
    public string UserId { get; set; }
}
