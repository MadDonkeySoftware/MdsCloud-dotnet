using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

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
}
