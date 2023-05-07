using Swashbuckle.AspNetCore.Annotations;

#pragma warning disable CS8618
namespace MdsCloud.Identity.Presentation.DTOs.Configuration;

public class ConfigurationResponseBody : Core.DTOs.Configuration
{
    [SwaggerSchema("")]
    public override bool AllowSelfSignCert { get; set; }

    [SwaggerSchema("")]
    public override string IdentityUrl { get; set; }

    [SwaggerSchema("")]
    public override string NsUrl { get; set; }

    [SwaggerSchema("")]
    public override string QsUrl { get; set; }

    [SwaggerSchema("")]
    public override string FsUrl { get; set; }

    [SwaggerSchema("")]
    public override string SfUrl { get; set; }

    [SwaggerSchema("")]
    public override string SmUrl { get; set; }
}
