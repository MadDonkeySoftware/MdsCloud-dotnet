using MdsCloud.SdkDotNet.Domain;

namespace MdsCloud.SdkDotNet;

public class SdkInitializeOptions : EnvironmentConfiguration
{
    public string? Token { get; set; }

    public bool AllUrlsPopulated()
    {
        return this.FileServiceUrl != null
            && this.IdentityUrl != null
            && this.NotificationServiceUrl != null
            && this.Password != null
            && this.QueueServiceUrl != null
            && this.ServerlessFunctionsServiceUrl != null
            && this.StateMachineServiceUrl != null;
    }

    public SdkInitializeOptions MergeWith(EnvironmentConfiguration? overrides)
    {
        if (overrides != null)
        {
            if (overrides.AccountId != null)
                this.AccountId = overrides.AccountId;
            if (overrides.AllowSelfSignCertificate != null)
                this.AllowSelfSignCertificate = overrides.AllowSelfSignCertificate;
            if (overrides.FileServiceUrl != null)
                this.FileServiceUrl = overrides.FileServiceUrl;
            if (overrides.IdentityUrl != null)
                this.IdentityUrl = overrides.IdentityUrl;
            if (overrides.NotificationServiceUrl != null)
                this.NotificationServiceUrl = overrides.NotificationServiceUrl;
            if (overrides.Password != null)
                this.Password = overrides.Password;
            if (overrides.QueueServiceUrl != null)
                this.QueueServiceUrl = overrides.QueueServiceUrl;
            if (overrides.ServerlessFunctionsServiceUrl != null)
                this.ServerlessFunctionsServiceUrl = overrides.ServerlessFunctionsServiceUrl;
            if (overrides.StateMachineServiceUrl != null)
                this.StateMachineServiceUrl = overrides.StateMachineServiceUrl;
            if (overrides.UserId != null)
                this.UserId = overrides.UserId;
        }
        return this;
    }

    public SdkInitializeOptions MergeWith(SdkInitializeOptions? overrides)
    {
        if (overrides != null)
        {
            if (overrides.AccountId != null)
                this.AccountId = overrides.AccountId;
            if (overrides.AllowSelfSignCertificate != null)
                this.AllowSelfSignCertificate = overrides.AllowSelfSignCertificate;
            if (overrides.FileServiceUrl != null)
                this.FileServiceUrl = overrides.FileServiceUrl;
            if (overrides.IdentityUrl != null)
                this.IdentityUrl = overrides.IdentityUrl;
            if (overrides.NotificationServiceUrl != null)
                this.NotificationServiceUrl = overrides.NotificationServiceUrl;
            if (overrides.Password != null)
                this.Password = overrides.Password;
            if (overrides.QueueServiceUrl != null)
                this.QueueServiceUrl = overrides.QueueServiceUrl;
            if (overrides.ServerlessFunctionsServiceUrl != null)
                this.ServerlessFunctionsServiceUrl = overrides.ServerlessFunctionsServiceUrl;
            if (overrides.StateMachineServiceUrl != null)
                this.StateMachineServiceUrl = overrides.StateMachineServiceUrl;
            if (overrides.Token != null)
                this.Token = overrides.Token;
            if (overrides.UserId != null)
                this.UserId = overrides.UserId;
        }
        return this;
    }
}
