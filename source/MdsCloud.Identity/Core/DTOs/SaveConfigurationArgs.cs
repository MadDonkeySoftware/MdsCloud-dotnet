namespace MdsCloud.Identity.Core.DTOs;

public class SaveConfigurationArgs
{
    public virtual bool? AllowSelfSignCert { get; set; }

    public virtual string? IdentityUrl { get; set; }

    public virtual string? NsUrl { get; set; }

    public virtual string? QsUrl { get; set; }

    public virtual string? FsUrl { get; set; }

    public virtual string? SfUrl { get; set; }

    public virtual string? SmUrl { get; set; }
}
