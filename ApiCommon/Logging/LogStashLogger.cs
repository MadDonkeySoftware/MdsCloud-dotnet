using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ApiCommon.Logging;

// TODO: pid, hostname, name,
internal class LogstashPayload
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

internal class RetryArguments
{
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public int Delay { get; private set; }

    internal RetryArguments()
        : this(5) { }

    internal RetryArguments(int maxAttempts)
    {
        Attempts = 1;
        Delay = 0;
        MaxAttempts = maxAttempts;
    }

    internal void Increment()
    {
        Attempts += 1;
        Delay = (int)(Math.Pow(2, Attempts) * 1000);
    }

    internal bool ShouldRetry()
    {
        return Attempts <= MaxAttempts;
    }
}

public class LogStashLogger : ILogger
{
    private readonly Func<MdsLoggerConfiguration> _getCurrentConfig;
    private readonly HttpClient _httpClient;

    public LogStashLogger(string name, Func<MdsLoggerConfiguration> getCurrentConfig)
    {
        _getCurrentConfig = getCurrentConfig;
        _httpClient = new HttpClient();
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
            PostLogstashPayload(
                new LogstashPayload
                {
                    Name = _getCurrentConfig().ServiceName,
                    Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    Level = ((int)logLevel + 1) * 10,
                    LogLevel = logLevel.ToString(),
                    Message = state != null ? state.ToString() : exception?.Message,
                }
            );
        }
    }

    private async void PostLogstashPayload(
        LogstashPayload payload,
        RetryArguments? retryArguments = null
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _getCurrentConfig().LogStashUrl);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Clear();
        request.Headers.Add("User-Agent", "MDS Cloud API Logger");

        var jsonBody = JsonConvert.SerializeObject(payload);
        request.Content = new StringContent(jsonBody);

        try
        {
            var response = await _httpClient.SendAsync(request);
            var retryStatuses = new List<HttpStatusCode>()
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.NotFound
            };
            if (!retryStatuses.Contains(response.StatusCode))
                return;

            var retry = retryArguments ?? new RetryArguments();
            if (!retry.ShouldRetry())
                return;

            retry.Increment();
            await Task.Delay(retry.Delay);
            PostLogstashPayload(payload, retry);
        }
        catch (Exception ex)
        {
            var retry = retryArguments ?? new RetryArguments();
            if (retry.ShouldRetry())
            {
                retry.Increment();
                await Task.Delay(retry.Delay);
                PostLogstashPayload(payload, retry);
            }
            else
            {
                throw;
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _getCurrentConfig().Enabled;
    }

    public IDisposable? BeginScope<TState>(TState state)
    {
        _httpClient.Dispose();
        return null;
    }
}
