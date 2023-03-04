using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MdsCloud.ApiCommon.Logging;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("MdsLogger")]
public class MdsLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
    private MdsLoggerConfiguration _config;

    public MdsLoggerProvider(IOptionsMonitor<MdsLoggerConfiguration> config)
    {
        _config = config.CurrentValue;
        _onChangeToken = config.OnChange(updates => _config = updates);
    }

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new LogStashLogger(name, GetCurrentConfig));

    private MdsLoggerConfiguration GetCurrentConfig() => _config;
}
