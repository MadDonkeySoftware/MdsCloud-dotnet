using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace Identity.DTOs.Configuration;

public class ConfigurationResponseBody
{
    [SwaggerSchema("")]
    public bool AllowSelfSignCert { get; set; }

    [SwaggerSchema("")]
    public string IdentityUrl { get; set; }

    [SwaggerSchema("")]
    public string NsUrl { get; set; }

    [SwaggerSchema("")]
    public string QsUrl { get; set; }

    [SwaggerSchema("")]
    public string FsUrl { get; set; }

    [SwaggerSchema("")]
    public string SfUrl { get; set; }

    [SwaggerSchema("")]
    public string SmUrl { get; set; }
}
