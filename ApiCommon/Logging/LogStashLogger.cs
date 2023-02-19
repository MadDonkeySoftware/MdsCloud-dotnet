using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ApiCommon.Logging;

internal class LogstashJsonPayload
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("@timestamp")]
    public string? Timestamp { get; set; }

    [JsonProperty("level")]
    public int? Level { get; set; }

    [JsonProperty("logLevel")]
    public string? LogLevel { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}

public class LogStashLogger : ILogger
{
    private readonly string _name;
    private readonly Func<MdsLoggerConfiguration> _getCurrentConfig;

    public LogStashLogger(string name, Func<MdsLoggerConfiguration> getCurrentConfig)
    {
        _getCurrentConfig = getCurrentConfig;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (IsEnabled(logLevel))
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Add("User-Agent", "MDS Cloud API Logger");
            // client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MDS Cloud API Logger"));

            // TODO: pid, hostname, name,
            var jsonBody = JsonConvert.SerializeObject(
                new LogstashJsonPayload
                {
                    Name = _getCurrentConfig().ServiceName,
                    Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    Level = ((int)logLevel + 1) * 10,
                    LogLevel = logLevel.ToString(),
                    Message = state != null ? state.ToString() : exception?.Message,
                }
            );
            // client.PostAsync(_getCurrentConfig().LogStashUrl, new StringContent(jsonBody));
            var postTask = client.PostAsync(
                _getCurrentConfig().LogStashUrl,
                new StringContent(jsonBody)
            );
            postTask.Wait();
            // var result = postTask.Result;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _getCurrentConfig().LogStashUrl != null;
    }

    public IDisposable? BeginScope<TState>(TState state)
    {
        return null;
    }
}
