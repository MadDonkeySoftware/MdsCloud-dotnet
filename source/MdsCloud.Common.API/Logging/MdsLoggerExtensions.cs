using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MdsCloud.Common.API.Logging;

public static class MdsLoggerExtensions
{
    public static ILoggingBuilder AddMdsLogStashLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, MdsLoggerProvider>()
        );

        LoggerProviderOptions.RegisterProviderOptions<
            MdsLoggerConfiguration,
            MdsLoggerConfiguration
        >(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddMdsLogStashLogger(
        this ILoggingBuilder builder,
        Action<MdsLoggerConfiguration> configure
    )
    {
        builder.AddMdsLogStashLogger();
        builder.Services.Configure(configure);

        return builder;
    }

    public static void LogWithMetadata(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        Dictionary<string, dynamic>? metadata = null
    )
    {
        if (!logger.IsEnabled(logLevel))
            return;
        var dict = new Dictionary<string, dynamic> { { "message", message } };

        if (metadata != null)
        {
            foreach (var key in metadata.Keys)
            {
                var value = metadata[key];
                if ((object?)value != null && key != "message")
                {
                    dict[key] = value!;
                }
            }
        }

        var payload = JsonConvert.SerializeObject(
            dict,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        );
        logger.Log(logLevel, payload);
    }

    public static void LogWithMetadata(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        object? metadata = null
    )
    {
        if (!logger.IsEnabled(logLevel))
            return;
        var dict = new Dictionary<string, dynamic> { { "message", message } };

        if (metadata != null)
        {
            AddObjectDataToDict(dict, metadata);
        }

        var payload = JsonConvert.SerializeObject(
            dict,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        );
        logger.Log(logLevel, payload);
    }

    public static void LogWithMetadata(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        string? mdsTraceId,
        object? metadata = null
    )
    {
        if (!logger.IsEnabled(logLevel))
            return;
        var dict = new Dictionary<string, dynamic>
        {
            { "message", message },
            { "mdsTraceId", mdsTraceId }
        };

        if (metadata != null)
        {
            AddObjectDataToDict(dict, metadata);
        }

        var payload = JsonConvert.SerializeObject(
            dict,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        );
        logger.Log(logLevel, payload);
    }

    public static string? GetMdsTraceId(this IHeaderDictionary headers)
    {
        return headers[LoggingConstants.TraceRequestHeaderKey][0];
    }

    public static void SetMdsTraceId(this IHeaderDictionary headers, string mdsTraceId)
    {
        headers[LoggingConstants.TraceRequestHeaderKey] = mdsTraceId;
    }

    public static string? GetMdsTraceId(this HttpRequest request)
    {
        return request.Headers.GetMdsTraceId();
    }

    public static void SetMdsTraceId(this HttpResponse response, string mdsTraceId)
    {
        response.Headers.SetMdsTraceId(mdsTraceId);
    }

    private static void AddObjectDataToDict(Dictionary<string, dynamic> dict, object metadata)
    {
        foreach (var propertyInfo in metadata.GetType().GetProperties())
        {
            var value = propertyInfo.GetValue(metadata);
            if (value != null && propertyInfo.Name != "message")
            {
                dict[propertyInfo.Name] = value!;
            }
        }
    }
}
