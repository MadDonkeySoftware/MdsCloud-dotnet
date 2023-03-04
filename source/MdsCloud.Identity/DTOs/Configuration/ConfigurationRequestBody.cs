using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.DTOs.Configuration;

public class ConfigurationBlock
{
    [SwaggerSchema("")]
    public bool? AllowSelfSignCert { get; set; }

    [SwaggerSchema("")]
    public string? IdentityUrl { get; set; }

    [SwaggerSchema("")]
    public string? NsUrl { get; set; }

    [SwaggerSchema("")]
    public string? QsUrl { get; set; }

    [SwaggerSchema("")]
    public string? FsUrl { get; set; }

    [SwaggerSchema("")]
    public string? SfUrl { get; set; }

    [SwaggerSchema("")]
    public string? SmUrl { get; set; }
}

public class ConfigurationRequestBody
{
    public ConfigurationBlock? Internal { get; set; }
    public ConfigurationBlock? External { get; set; }
}
