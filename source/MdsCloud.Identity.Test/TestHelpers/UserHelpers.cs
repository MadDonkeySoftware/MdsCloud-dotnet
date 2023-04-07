using System.Net;
using MdsCloud.Identity.DTOs.Registration;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test.TestHelpers;

internal class TestUserLoginDetails
{
    public string AccountId { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

internal static class UserHelpers
{
    internal static async Task<TestUserLoginDetails> CreateTestUser(HttpClient client)
    {
        var testUsername = $"User_{RandomStringGenerator.GenerateString(8)}";
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var friendlyName = $"Test {testUsername}";
        var accountName = $"Account for {testUsername}";

        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Password = testPassword,
                        Email = testEmail,
                        FriendlyName = friendlyName,
                        AccountName = accountName
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        var body = response!.Content.ReadAsStringAsync().Result;
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new Exception($"Could not create test user: ${body}");
        }

        var jsonResponseObj = JsonConvert.DeserializeObject<RegistrationResponseBody>(body);

        return new TestUserLoginDetails
        {
            Password = testPassword,
            UserName = testUsername,
            AccountId = jsonResponseObj!.AccountId!,
        };
    }
}
