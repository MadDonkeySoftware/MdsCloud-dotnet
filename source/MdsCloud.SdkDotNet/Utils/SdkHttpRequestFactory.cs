using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Utils;

internal class CreateRequestArgs
{
    internal string? EnvironmentName { get; set; }
    internal IAuthManager? AuthManager { get; set; }
    internal IConfigUtilities? ConfigUtilities { get; set; }
    internal bool? AllowSelfSignCert { get; set; }
    internal HttpMethod? HttpMethod { get; set; }
    internal string? Url { get; set; }
    internal HttpContent? Content { get; set; }
}

internal class SdkHttpRequestFactory
{
    internal async Task<HttpResponseMessage?> MakeRequest(CreateRequestArgs args)
    {
        using var handler = new HttpClientHandler();
        if (args.AllowSelfSignCert ?? false)
        {
            handler.ServerCertificateCustomValidationCallback += (
                message,
                certificate,
                chain,
                sslPolicyErrors
            ) => true;
        }
        using var client = new HttpClient(handler);
        using var request = await CreateBaseRequest(args);

        return client.Send(request);
    }

    internal async Task<T> MakeRequest<T>(CreateRequestArgs args)
    {
        var response = await MakeRequest(args);
        if (response == null)
        {
            throw new Exception("Failed to obtain response from service");
        }
        var body = response.Content.ReadAsStringAsync().Result;
        var responsePayload = JsonConvert.DeserializeObject<T>(body);
        return responsePayload ?? throw new Exception("Failed to parse response from service");
    }

    internal async Task<HttpRequestMessage> CreateBaseRequest(CreateRequestArgs args)
    {
        if (
            (args.HttpMethod == null && args.Url != null)
            || (args.HttpMethod != null && args.Url == null)
        )
            throw new ArgumentException(
                "Both HttpMethod and Url must be provided together or both omitted"
            );

        HttpRequestMessage request;
        if (args.HttpMethod != null && args.Url != null)
        {
            request = new HttpRequestMessage(args.HttpMethod, args.Url);
        }
        else
        {
            request = new HttpRequestMessage();
        }

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Clear();
        request.Headers.Add("User-Agent", "MDS Cloud Sdk Dotnet");

        if (args.Content != null)
        {
            request.Content = args.Content;
        }

        if (args.AuthManager != null)
        {
            string token;
            if (args.EnvironmentName != null && args.ConfigUtilities != null)
            {
                var conf = args.ConfigUtilities.GetConfig(args.EnvironmentName);
                token = await args.AuthManager.GetAuthenticationToken(
                    conf.AccountId,
                    conf.UserId,
                    conf.Password
                );
            }
            else
            {
                token = await args.AuthManager.GetAuthenticationToken();
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }
}
