using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using MdsCloud.SdkDotNet.Extensions;
using MdsCloud.SdkDotNet.Utils.Cache;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Utils;

internal class AuthenticationPayload
{
    [JsonProperty("accountId")]
    public string? AccountId { get; set; }

    [JsonProperty("userId")]
    public string? UserId { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }
}

internal class TokenPayload
{
    [JsonProperty("token")]
    public string? Token { get; set; }
}

public class AuthManager : IAuthManager
{
    private readonly SdkHttpRequestFactory _sdkHttpRequestFactory = new SdkHttpRequestFactory();
    private readonly ITokenCache _cache;
    private readonly HttpClient _httpClient;
    private readonly string _identityUrl;
    private readonly string _password;
    private readonly bool _allowSelfSignCert;

    private string UserId { get; set; }
    private string Account { get; set; }

    public AuthManager(
        ITokenCache cache,
        string identityUrl,
        string userId,
        string password,
        string account,
        bool allowSelfSignCert
    )
    {
        // TODO: Make interface in SystemWrappers
        _httpClient = new HttpClient();

        _cache = cache;
        _identityUrl = identityUrl;
        UserId = userId;
        _password = password;
        Account = account;
        _allowSelfSignCert = allowSelfSignCert;
    }

    private async Task<string> GetNewToken(string accountId, string userId, string password)
    {
        var url = Flurl.Url.Combine(_identityUrl, "v1", "authenticate");
        var requestPayload = JsonConvert.SerializeObject(
            new AuthenticationPayload
            {
                AccountId = accountId,
                Password = password,
                UserId = userId
            }
        );

        var factory = new SdkHttpRequestFactory();
        var response = await factory.MakeRequest(
            new CreateRequestArgs()
            {
                HttpMethod = HttpMethod.Post,
                Url = url,
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json"),
                AllowSelfSignCert = _allowSelfSignCert,
            }
        );

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<TokenPayload>(responseBody)?.Token;
            if (token == null)
            {
                throw new Exception("Failed to obtain new token");
            }
            return token;
        }
        else
        {
            // TODO: specific exception
            throw new Exception("Failed to get new token");
        }
    }

    private string GetCacheKey(string accountId, string userId) =>
        $"{this._identityUrl}|{accountId}|{userId}";

    private JwtSecurityToken? ReadTokenInsecure(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var secureToken = handler.ReadToken(token) as JwtSecurityToken;
        return secureToken;
    }

    public Task<string> GetAuthenticationToken() =>
        this.GetAuthenticationToken(this.Account, this.UserId, this._password);

    public async Task<string> GetAuthenticationToken(
        string accountId,
        string userId,
        string password
    )
    {
        var cacheKey = GetCacheKey(accountId, userId);
        var cacheValue = this._cache.Get(cacheKey);
        if (cacheValue != null)
        {
            var cachedToken = ReadTokenInsecure(cacheValue);

            if (cachedToken != null)
            {
                // NOTE: Add a 60 second buffer to ensure calls will succeed.
                var nowEpoch = DateTime.Now.ToUnixTimestamp() + 60;
                var expirationClaim = cachedToken.Claims.First(c => c.Type == "exp");
                var expiration = double.TryParse(expirationClaim.Value, out var exp) ? exp : 0;
                if (nowEpoch < expiration)
                {
                    return cacheValue;
                }
            }
        }

        var token = await GetNewToken(accountId, userId, password);
        this.SetAuthenticationToken(token);
        return token;
    }

    public void SetAuthenticationToken(string token)
    {
        var jwt = ReadTokenInsecure(token);
        if (jwt != null)
        {
            var accountId = jwt.Claims.First(c => c.Type == "accountId").Value;
            var userId = jwt.Claims.First(c => c.Type == "userId").Value;

            // We "seed" these for when the MDS SDK is configured via the docker minion
            this.Account = accountId;
            this.UserId = userId;

            var cacheKey = GetCacheKey(accountId, userId);
            this._cache.Set(cacheKey, token);
        }
    }
}
