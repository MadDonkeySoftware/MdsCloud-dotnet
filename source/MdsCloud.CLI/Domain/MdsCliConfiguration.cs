#pragma warning disable CS8618
using System.Reflection;
using MdsCloud.CLI.Attributes;
using MdsCloud.SdkDotNet.Domain;

namespace MdsCloud.CLI.Domain;

public class MdsCliConfiguration : EnvironmentConfiguration
{
    private readonly EnvironmentConfiguration _environmentConfiguration;

    public MdsCliConfiguration()
        : this(new EnvironmentConfiguration()) { }

    public MdsCliConfiguration(EnvironmentConfiguration config)
    {
        _environmentConfiguration = config;
    }

    [ConfigElementDisplaySettings(
        Key = "account",
        DisplayPrompt = "Account",
        QueryPrompt = "Enter your account:",
        DisplayOrder = 1
    )]
    public override string? AccountId
    {
        get => _environmentConfiguration.AccountId;
        set => _environmentConfiguration.AccountId = value;
    }

    [ConfigElementDisplaySettings(
        Key = "userId",
        DisplayPrompt = "User Id",
        QueryPrompt = "Enter your user id:",
        DisplayOrder = 2
    )]
    public override string? UserId
    {
        get => _environmentConfiguration.UserId;
        set => _environmentConfiguration.UserId = value;
    }

    [ConfigElementDisplaySettings(
        Key = "password",
        DisplayPrompt = "Password",
        HideValue = true,
        QueryPrompt = "Enter your password:",
        DisplayOrder = 3
    )]
    public override string? Password
    {
        get => _environmentConfiguration.Password;
        set => _environmentConfiguration.Password = value;
    }

    [ConfigElementDisplaySettings(
        Key = "identityUrl",
        DisplayPrompt = "MdsCloud.Identity Service Url",
        QueryPrompt = "Enter url for the identity service:",
        DisplayOrder = 4
    )]
    public override string? IdentityUrl
    {
        get => _environmentConfiguration.IdentityUrl;
        set => _environmentConfiguration.IdentityUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "nsUrl",
        DisplayPrompt = "Notification Service Url",
        QueryPrompt = "Enter url for the notification service:",
        DisplayOrder = 6
    )]
    public override string? NotificationServiceUrl
    {
        get => _environmentConfiguration.NotificationServiceUrl;
        set => _environmentConfiguration.NotificationServiceUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "qsUrl",
        DisplayPrompt = "Queue Service Url",
        QueryPrompt = "Enter url for the queue service:",
        DisplayOrder = 7
    )]
    public override string? QueueServiceUrl
    {
        get => _environmentConfiguration.QueueServiceUrl;
        set => _environmentConfiguration.QueueServiceUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "fsUrl",
        DisplayPrompt = "File Service Url",
        QueryPrompt = "Enter url for the file service:",
        DisplayOrder = 8
    )]
    public override string? FileServiceUrl
    {
        get => _environmentConfiguration.FileServiceUrl;
        set => _environmentConfiguration.FileServiceUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "sfUrl",
        DisplayPrompt = "Serverless Functions Service Url",
        QueryPrompt = "Enter url for the serverless function service:",
        DisplayOrder = 9
    )]
    public override string? ServerlessFunctionsServiceUrl
    {
        get => _environmentConfiguration.ServerlessFunctionsServiceUrl;
        set => _environmentConfiguration.ServerlessFunctionsServiceUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "smUrl",
        DisplayPrompt = "State Machine Service Url",
        QueryPrompt = "Enter url for the state machine service:",
        DisplayOrder = 10
    )]
    public override string? StateMachineServiceUrl
    {
        get => _environmentConfiguration.StateMachineServiceUrl;
        set => _environmentConfiguration.StateMachineServiceUrl = value;
    }

    [ConfigElementDisplaySettings(
        Key = "allowSelfSignCert",
        DisplayPrompt = "Allow self-signed Certificates",
        QueryPrompt = "Allow self-signed certificates when communicating with the identity service?",
        DisplayOrder = 5
    )]
    public override bool? AllowSelfSignCertificate
    {
        get => _environmentConfiguration.AllowSelfSignCertificate;
        set => _environmentConfiguration.AllowSelfSignCertificate = value;
    }
}
