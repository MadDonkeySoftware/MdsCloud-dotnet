using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MdsCloud.SdkDotNet.Clients;
using MdsCloud.SdkDotNet.Domain;
using MdsCloud.SdkDotNet.Utils;
using MdsCloud.SdkDotNet.Utils.Cache;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet;

public class MdsSdk
{
    private static MdsSdk? _instance;
    private static IAuthManager? _authManager;
    private static Utilities _utilities = new Utilities();

    private string? Account { get; }
    private bool? AllowSelfSignCert { get; }
    private string? FsUrl { get; }
    private string? IdentityUrl { get; }
    private string? NsUrl { get; }
    private string? Password { get; }
    private string? QsUrl { get; }
    private string? SfUrl { get; }
    private string? SmUrl { get; }
    private string? UserId { get; }

    private MdsSdk(SdkInitializeOptions args)
    {
        this.Account = args.AccountId;
        this.AllowSelfSignCert = args.AllowSelfSignCertificate;
        this.FsUrl = args.FileServiceUrl;
        this.IdentityUrl = args.IdentityUrl;
        this.NsUrl = args.NotificationServiceUrl;
        this.Password = args.Password;
        this.QsUrl = args.QueueServiceUrl;
        this.SfUrl = args.ServerlessFunctionsServiceUrl;
        this.SmUrl = args.StateMachineServiceUrl;
        this.UserId = args.UserId;
    }

    private static SdkInitializeOptions? GetSettings()
    {
        if (_instance == null)
            return null;
        return new SdkInitializeOptions
        {
            AccountId = _instance.Account,
            AllowSelfSignCertificate = _instance.AllowSelfSignCert,
            FileServiceUrl = _instance.FsUrl,
            IdentityUrl = _instance.IdentityUrl,
            NotificationServiceUrl = _instance.NsUrl,
            Password = _instance.Password,
            QueueServiceUrl = _instance.QsUrl,
            ServerlessFunctionsServiceUrl = _instance.SfUrl,
            StateMachineServiceUrl = _instance.SmUrl,
            UserId = _instance.UserId,
        };
    }

    private static async Task<EnvironmentConfiguration> GetConfigurationFromIdentity(
        string? identityUrl,
        bool allowSelfSignCert = true
    )
    {
        try
        {
            var url = Flurl.Url.Combine(identityUrl, "v1", "configuration");

            var sdkHttpFactory = new SdkHttpRequestFactory();
            var config = await sdkHttpFactory.MakeRequest<EnvironmentConfiguration>(
                new CreateRequestArgs
                {
                    AllowSelfSignCert = allowSelfSignCert,
                    HttpMethod = HttpMethod.Get,
                    Url = url
                }
            );

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get config from Identity");
            return new EnvironmentConfiguration();
        }
    }

    private static IAuthManager GetAuthManager(string? environmentKey = null)
    {
        if (_authManager == null)
        {
            var env = environmentKey ?? _utilities.ConfigUtilities.GetDefaultEnvironment();
            var envConfig = _utilities.ConfigUtilities.GetConfig(env);
            _authManager = new AuthManager(
                new DiscTokenCache(),
                envConfig.IdentityUrl ?? string.Empty,
                envConfig.UserId ?? string.Empty,
                envConfig.Password ?? string.Empty,
                envConfig.AccountId ?? string.Empty,
                envConfig.AllowSelfSignCertificate ?? false
            );
        }

        return _authManager;
    }

    public static void Initialize()
    {
        var env = _utilities.ConfigUtilities.GetDefaultEnvironment();
        Initialize(env);
    }

    public static void Initialize(string environmentKey)
    {
        if (string.IsNullOrEmpty(environmentKey))
        {
            throw new ArgumentException(
                "Environment key is expected to be a non-null or empty string value",
                nameof(environmentKey)
            );
        }

        var envConfig = _utilities.ConfigUtilities.GetConfig(environmentKey);
        Initialize(
            new SdkInitializeOptions
            {
                AccountId = envConfig.AccountId,
                Password = envConfig.Password,
                IdentityUrl = envConfig.IdentityUrl,
                AllowSelfSignCertificate = envConfig.AllowSelfSignCertificate,
                UserId = envConfig.UserId,
                FileServiceUrl = envConfig.FileServiceUrl,
                NotificationServiceUrl = envConfig.NotificationServiceUrl,
                QueueServiceUrl = envConfig.QueueServiceUrl,
                ServerlessFunctionsServiceUrl = envConfig.ServerlessFunctionsServiceUrl,
                StateMachineServiceUrl = envConfig.StateMachineServiceUrl,
            }
        );
    }

    public static async void Initialize(SdkInitializeOptions args)
    {
        _authManager = null;
        var configData = new SdkInitializeOptions();

        if (!args.AllUrlsPopulated())
        {
            var oldConfig = MdsSdk.GetSettings();
            var autoConfig = await GetConfigurationFromIdentity(args.IdentityUrl);
            configData.MergeWith(oldConfig).MergeWith(autoConfig);
        }

        configData.MergeWith(args);

        // TODO: Hide password value?
        _utilities.VerboseWrite("Config Data");
        _utilities.VerboseWrite(JsonConvert.SerializeObject(configData, Formatting.Indented));

        _instance = new MdsSdk(configData);
        _authManager = new AuthManager(
            new DiscTokenCache(),
            configData.IdentityUrl ?? string.Empty,
            configData.UserId ?? string.Empty,
            configData.Password ?? string.Empty,
            configData.AccountId ?? string.Empty,
            configData.AllowSelfSignCertificate ?? false
        );

        if (configData.Token != null)
        {
            _authManager.SetAuthenticationToken(configData.Token);
        }
    }

    // Clients
    /*
    public static async getFileServiceClient() {
        return new FileServiceClient(
            this.instance.fsUrl,
            await this.getAuthManager(),
        );
    }

    public static async getQueueServiceClient() {
        return new QueueServiceClient(
            this.instance.qsUrl,
            await this.getAuthManager(),
        );
    }

    public static async getStateMachineServiceClient() {
        return new StateMachineServiceClient(
            this.instance.smUrl,
            await this.getAuthManager(),
        );
    }

    public static async getNotificationServiceClient() {
        return new NotificationServiceClient(
            this.instance.nsUrl,
            await this.getAuthManager(),
        );
    }

    public static async getServerlessFunctionsClient() {
        return new ServerlessFunctionsClient(
            this.instance.sfUrl,
            await this.getAuthManager(),
        );
    }

    public static async getIdentityServiceClient() {
        return new IdentityServiceClient(
            this.instance.identityUrl,
            await this.getAuthManager(),
            this.instance.allowSelfSignCert,
        );
    }
    */

    public static IdentityServiceClient GetIdentityServiceClient()
    {
        if (_instance == null)
            throw new Exception("Not Initialized");
        if (_instance.IdentityUrl == null)
            throw new ArgumentException(
                "Identity service url has not be initialized properly. Ensure config contains value for this url"
            );

        return new IdentityServiceClient(
            _instance.IdentityUrl,
            GetAuthManager(),
            _instance.AllowSelfSignCert ?? false
        );
    }
}
