using System.Transactions;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Core.Lookups;
using MdsCloud.Identity.Core.Model;

namespace MdsCloud.Identity.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger _logger;
    private readonly ILandscapeUrlRepository _landscapeUrlRepository;
    private readonly ISettings _settings;
    private readonly IFile _file;

    public ConfigurationService(
        ILogger logger,
        ILandscapeUrlRepository landscapeUrlRepository,
        ISettings settings,
        IFile file
    )
    {
        _logger = logger;
        _landscapeUrlRepository = landscapeUrlRepository;
        _settings = settings;
        _file = file;
    }

    public Configuration GetConfiguration(ArgsWithTrace<string> requestArgs)
    {
        var elements = _landscapeUrlRepository.GetLandscapeUrlsForScope(requestArgs.Data);
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
        using var transaction = new TransactionScope();

        foreach (var set in args.Data)
        {
            var (scope, config) = set;

            if (config.AllowSelfSignCert.HasValue)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(
                        scope,
                        LandscapeUrlKeys.AllowSelfSignCert,
                        config.AllowSelfSignCert.Value.ToString()
                    )
                );
            }

            if (config.IdentityUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.IdentityUrl, config.IdentityUrl)
                );
            }

            if (config.FsUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.FsUrl, config.FsUrl)
                );
            }

            if (config.NsUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.NsUrl, config.NsUrl)
                );
            }

            if (config.QsUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.QsUrl, config.QsUrl)
                );
            }

            if (config.SfUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.SfUrl, config.SfUrl)
                );
            }

            if (config.SmUrl != null)
            {
                _landscapeUrlRepository.SaveLandscapeUrl(
                    new LandscapeUrl(scope, LandscapeUrlKeys.SmUrl, config.SmUrl)
                );
            }
        }

        transaction.Complete();
        transaction.Dispose();
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
