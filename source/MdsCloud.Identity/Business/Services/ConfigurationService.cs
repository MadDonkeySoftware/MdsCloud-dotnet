using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Exceptions;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Domain.Lookups;
using MdsCloud.Identity.Settings;
using NHibernate;

namespace MdsCloud.Identity.Business.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly ISettings _settings;
    private readonly IFile _file;

    public ConfigurationService(
        ILogger logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IFile file
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _settings = settings;
        _file = file;
    }

    public Configuration GetConfiguration(ArgWithTrace<string> requestArgs)
    {
        using var session = _sessionFactory.OpenSession();
        var elements = session.Query<LandscapeUrl>().Where(e => e.Scope == requestArgs.Arg);
        var config = new Configuration();

        foreach (var landscapeUrl in elements)
        {
            switch (landscapeUrl.Key)
            {
                case "allowSelfSignCert":
                    config.AllowSelfSignCert = bool.Parse(landscapeUrl.Value);
                    break;
                case "fsUrl":
                    config.FsUrl = landscapeUrl.Value;
                    break;
                case "identityUrl":
                    config.IdentityUrl = landscapeUrl.Value;
                    break;
                case "nsUrl":
                    config.NsUrl = landscapeUrl.Value;
                    break;
                case "qsUrl":
                    config.QsUrl = landscapeUrl.Value;
                    break;
                case "sfUrl":
                    config.SfUrl = landscapeUrl.Value;
                    break;
                case "smUrl":
                    config.SmUrl = landscapeUrl.Value;
                    break;
            }
        }

        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Successfully retrieved configuration data",
            requestArgs.MdsTraceId,
            config
        );
        return config;
    }

    public void SaveConfigurations(ArgsWithTrace<List<Tuple<string, SaveConfigurationArgs>>> args)
    {
        _logger.LogWithMetadata(
            LogLevel.Trace,
            "Updating configuration data.",
            args.MdsTraceId,
            args.Data
        );

        // TODO: Determine if request is from local address or external address
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        foreach (var set in args.Data)
        {
            var (scope, config) = set;

            if (config.AllowSelfSignCert.HasValue)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        scope,
                        LandscapeUrlKeys.AllowSelfSignCert,
                        config.AllowSelfSignCert.Value.ToString()
                    )
                );
            }

            if (config.IdentityUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(scope, LandscapeUrlKeys.IdentityUrl, config.IdentityUrl)
                );
            }

            if (config.FsUrl != null)
            {
                session.SaveOrUpdate(new LandscapeUrl(scope, LandscapeUrlKeys.FsUrl, config.FsUrl));
            }

            if (config.NsUrl != null)
            {
                session.SaveOrUpdate(new LandscapeUrl(scope, LandscapeUrlKeys.NsUrl, config.NsUrl));
            }

            if (config.QsUrl != null)
            {
                session.SaveOrUpdate(new LandscapeUrl(scope, LandscapeUrlKeys.QsUrl, config.QsUrl));
            }

            if (config.SfUrl != null)
            {
                session.SaveOrUpdate(new LandscapeUrl(scope, LandscapeUrlKeys.SfUrl, config.SfUrl));
            }

            if (config.SmUrl != null)
            {
                session.SaveOrUpdate(new LandscapeUrl(scope, LandscapeUrlKeys.SmUrl, config.SmUrl));
            }
        }

        transaction.Commit();
    }

    public string GetPublicSignature()
    {
        var keyPath = _settings["MdsSettings:Secrets:PublicPath"];
        if (keyPath == null)
        {
            throw new InvalidSettingsValueException(
                "MdsSettings:Secrets:PublicPath cannot be null"
            );
        }

        var publicKeyText = _file.ReadAllText(keyPath);
        return publicKeyText;
    }
}
