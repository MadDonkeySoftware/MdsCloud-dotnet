namespace MdsCloud.Identity.Business.DTOs;

public class Configuration
{
    public virtual bool AllowSelfSignCert { get; set; }

    public virtual string IdentityUrl { get; set; } = string.Empty;

    public virtual string NsUrl { get; set; } = string.Empty;

    public virtual string QsUrl { get; set; } = string.Empty;

    public virtual string FsUrl { get; set; } = string.Empty;

    public virtual string SfUrl { get; set; } = string.Empty;

    public virtual string SmUrl { get; set; } = string.Empty;
}
