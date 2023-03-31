using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MdsCloud.Common.API.Logging;

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
            var stringMessage = state != null ? state.ToString() : exception?.Message;

            PostLogstashPayload(GetPayload(stringMessage, logLevel));
        }
    }

    private string GetPayload(string input, LogLevel logLevel)
    {
        // TODO: pid, hostname, name,
        var dict = new Dictionary<string, dynamic>
        {
            { "event.dataset", _getCurrentConfig().ServiceName },
            { "@timestamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
            { "level", ((int)logLevel + 1) * 10 },
            { "logLevel", logLevel.ToString() },
            // { "message", "" },  // This is populated below. Leaving commented here so it is easy to see the template.
        };
        var restrictedKeys = dict.Keys;

        try
        {
            var userDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(input);
            foreach (var key in userDict.Keys)
            {
                if (userDict[key] != null && !restrictedKeys.Contains(key))
                {
                    if (key == "message")
                    {
                        dict[key] = userDict[key];
                    }
                    else
                    {
                        dict[$"metadata.{key}"] = userDict[key];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            dict["message"] = input;
        }

        return JsonConvert.SerializeObject(
            dict,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        );
    }

    private async void PostLogstashPayload(string payload, RetryArguments? retryArguments = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _getCurrentConfig().LogStashUrl);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Clear();
        request.Headers.Add("User-Agent", "MDS Cloud API Logger");

        request.Content = new StringContent(payload);

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
        catch (Exception)
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
                Console.WriteLine("==========================");
                Console.WriteLine("Issue logging to logstash.");
                Console.WriteLine("==========================");
                throw;
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _getCurrentConfig().Enabled;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        _httpClient.Dispose();
        return null;
    }
}
