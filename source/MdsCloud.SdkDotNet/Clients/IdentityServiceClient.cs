using System.Net;
using System.Text;
using MdsCloud.SdkDotNet.DTOs;
using MdsCloud.SdkDotNet.DTOs.Identity;
using MdsCloud.SdkDotNet.Utils;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Clients;

public class IdentityServiceClient
{
    public string ServiceUrl { get; }
    public bool AllowSelfSignCert { get; }

    protected IAuthManager AuthManager { get; }

    private SdkHttpRequestFactory RequestFactory { get; }

    public IdentityServiceClient(
        string serviceUrl,
        IAuthManager authManager,
        bool allowSelfSignCert
    )
    {
        ServiceUrl = serviceUrl;
        AuthManager = authManager;
        AllowSelfSignCert = allowSelfSignCert;
        RequestFactory = new SdkHttpRequestFactory();
    }

    public async Task<RegistrationResponse?> Register(RegisterRequest args)
    {
        var url = Flurl.Url.Combine(this.ServiceUrl, "v1", "register");
        var response = await RequestFactory.MakeRequest(
            new CreateRequestArgs
            {
                AllowSelfSignCert = this.AllowSelfSignCert,
                HttpMethod = HttpMethod.Post,
                Url = url,
                Content = new StringContent(
                    JsonConvert.SerializeObject(args),
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("An error occurred while updating the user");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<RegistrationResponse>(responseBody);
    }

    public Task<string> Authenticate()
    {
        return AuthManager.GetAuthenticationToken();
    }

    public Task<string> Authenticate(string accountId, string userId, string password)
    {
        return AuthManager.GetAuthenticationToken(accountId, userId, password);
    }

    public async Task UpdateUser(UpdateUserRequest args)
    {
        var url = Flurl.Url.Combine(this.ServiceUrl, "v1", "updateUser");
        var response = await RequestFactory.MakeRequest(
            new CreateRequestArgs
            {
                AllowSelfSignCert = this.AllowSelfSignCert,
                HttpMethod = HttpMethod.Post,
                Url = url,
                Content = new StringContent(
                    JsonConvert.SerializeObject(args),
                    Encoding.UTF8,
                    "application/json"
                ),
                AuthManager = this.AuthManager,
            }
        );

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("An error occurred while updating the user");
        }
    }

    public async Task<string?> GetPublicSignature()
    {
        var url = Flurl.Url.Combine(this.ServiceUrl, "v1", "publicSignature");
        var responsePayload = await RequestFactory.MakeRequest<PublicSignatureResponse>(
            new CreateRequestArgs()
            {
                AllowSelfSignCert = this.AllowSelfSignCert,
                HttpMethod = HttpMethod.Get,
                Url = url
            }
        );

        return responsePayload.Signature;
    }

    public void ImpersonateUser()
    {
        throw new NotImplementedException();
    }
}
