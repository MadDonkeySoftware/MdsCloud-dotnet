using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MdsCloud.Identity.Test.TestHelpers;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test;

internal class CreateRequestArgs
{
    internal string? AuthToken { get; set; }
    internal bool? AllowSelfSignCert { get; set; }
    internal HttpMethod? HttpMethod { get; set; }
    internal string? Url { get; set; }
    internal HttpContent? Content { get; set; }
}

internal static class TestHttpRequestFactory
{
    internal static HttpContent CreateJsonContent(object data)
    {
        return new StringContent(
            JsonConvert.SerializeObject(data),
            Encoding.UTF8,
            "application/json"
        );
    }

    internal static JwtSecurityToken GetJwtToken(string jwt)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(TestConstants.TestPublicPemData);

        var validationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidIssuer = TestConstants.TestJwtIssuer,
            ValidateAudience = true,
            ValidAudience = TestConstants.TestJwtIssuer,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuerSigningKey = true,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(jwt, validationParameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwtValidatedToken)
            throw new Exception("Failed to coerce security token to jwt security token");
        return jwtValidatedToken;
    }

    internal static async Task<HttpResponseMessage?> MakeRequest(
        HttpClient client,
        CreateRequestArgs args
    )
    {
        using var request = CreateBaseRequest(args);
        return await client.SendAsync(request);
    }

    internal static Task<HttpResponseMessage?> MakeRequest(CreateRequestArgs args)
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
        return MakeRequest(client, args);
    }

    internal static async Task<T> MakeRequest<T>(CreateRequestArgs args)
    {
        var response = await MakeRequest(args);
        var body = response.Content.ReadAsStringAsync().Result;
        var responsePayload = JsonConvert.DeserializeObject<T>(body);
        return responsePayload;
    }

    internal static HttpRequestMessage CreateBaseRequest(CreateRequestArgs args)
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

        if (args.AuthToken != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", args.AuthToken);
        }

        return request;
    }
}
