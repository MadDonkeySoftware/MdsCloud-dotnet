using System.Net;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using MdsCloud.Identity.UI.Controllers.V1;
using MdsCloud.Identity.UI.DTOs.Authentication;
using MdsCloud.Identity.UI.DTOs.Configuration;
using MdsCloud.Identity.UI.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace MdsCloud.Identity.Test.Integration;

public class ConfigurationTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    private readonly Mock<IFile> _fileMock = new();
    private readonly Mock<ISettings> _settingsMock = new();
    private readonly Mock<IRequestUtilities> _requestUtilitiesMock = new();
    private readonly Mock<ILogger<UserController>> _logger = new();

    public ConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
        var dbBuilder = new IdentityDatabaseBuilder();
        dbBuilder.Initialize().Wait();

        _factory = new IdentityWebApplicationFactory();
        _factory.DbConnectionString = dbBuilder.TestDbConnectionString;

        _output.WriteLine("DbName: " + dbBuilder.DbName);

        var testRequestUtilities = new TestRequestUtilities(_settingsMock.Object, _fileMock.Object);
        _requestUtilitiesMock
            .Setup(o => o.GetRequestJwt(It.IsAny<string>()))
            .Returns<string>(header => testRequestUtilities.GetRequestJwt(header));

        _factory.OnConfigureServices += (_, services) =>
        {
            services.AddSingleton<IFile>(_fileMock.Object);
            services.AddSingleton<ISettings>(_settingsMock.Object);
            services.AddSingleton<IRequestUtilities>(_requestUtilitiesMock.Object);
            services.AddSingleton<ILogger<UserController>>(_logger.Object);
            services.AddScoped<IConnectionFactory>(
                provider => new ConnectionFactory(dbBuilder.TestDbConnectionString)
            );
        };
    }

    public void Dispose()
    {
        // Dispose runs after each test.
        GC.SuppressFinalize(this);
    }

    private async Task<string> GetAuthToken(
        HttpClient client,
        string accountId,
        string userId,
        string password
    )
    {
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        AccountId = accountId,
                        UserId = userId,
                        Password = password
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);
        var body = JsonConvert.DeserializeObject<AuthenticationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        if (body == null)
        {
            throw new NullReferenceException("Unable to obtain authentication result");
        }

        return body.Token;
    }

    private async Task<string> GetAdminAuthToken(HttpClient client)
    {
        return await GetAuthToken(
            client,
            "1",
            "mdsCloud",
            TestConstants.TestMdsSystemRootUserPassword
        );
    }

    [Fact(DisplayName = "When no scope specified returns internal scope values")]
    public async Task GET_when_no_scope_gets_internal_values()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Url = "/v1/configuration",
                HttpMethod = HttpMethod.Get,
                AllowSelfSignCert = true
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonConvert.DeserializeObject<ConfigurationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        Assert.Equal("http://mds-identity:8888", body.IdentityUrl);
        Assert.Equal("http://mds-ns:8888", body.NsUrl);
        Assert.Equal("http://mds-qs:8888", body.QsUrl);
        Assert.Equal("http://mds-fs:8888", body.FsUrl);
        Assert.Equal("http://mds-sf:8888", body.SfUrl);
        Assert.Equal("http://mds-sm:8888", body.SmUrl);
        Assert.True(body.AllowSelfSignCert);
    }

    [Fact(DisplayName = "When internal scope specified returns internal scope values")]
    public async Task GET_when_internal_scope_gets_internal_values()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Url = "/v1/configuration?scope=internal",
                HttpMethod = HttpMethod.Get,
                AllowSelfSignCert = true
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonConvert.DeserializeObject<ConfigurationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        Assert.Equal("http://mds-identity:8888", body.IdentityUrl);
        Assert.Equal("http://mds-ns:8888", body.NsUrl);
        Assert.Equal("http://mds-qs:8888", body.QsUrl);
        Assert.Equal("http://mds-fs:8888", body.FsUrl);
        Assert.Equal("http://mds-sf:8888", body.SfUrl);
        Assert.Equal("http://mds-sm:8888", body.SmUrl);
        Assert.True(body.AllowSelfSignCert);
    }

    [Fact(DisplayName = "When external scope specified returns internal scope values")]
    public async Task GET_when_external_scope_gets_internal_values()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Url = "/v1/configuration?scope=external",
                HttpMethod = HttpMethod.Get,
                AllowSelfSignCert = true
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonConvert.DeserializeObject<ConfigurationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        Assert.Equal("https://127.0.0.1:8081", body.IdentityUrl);
        Assert.Equal("http://127.0.0.1:8082", body.NsUrl);
        Assert.Equal("http://127.0.0.1:8083", body.QsUrl);
        Assert.Equal("http://127.0.0.1:8084", body.FsUrl);
        Assert.Equal("http://127.0.0.1:8085", body.SfUrl);
        Assert.Equal("http://127.0.0.1:8086", body.SmUrl);
        Assert.True(body.AllowSelfSignCert);
    }

    [Fact(DisplayName = "POST updates configs accordingly when admin")]
    public async Task POST_updates_both_configs_accordingly()
    {
        // Arrange
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key"))))
            .Returns(TestConstants.TestPrivateKeyData);
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key.pub.pem"))))
            .Returns(TestConstants.TestPublicPemData);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePath"])
            .Returns((string key) => "/some/path/to/key");
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePassword"])
            .Returns((string key) => TestConstants.TestPrivateKeyPassword);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PublicPath"])
            .Returns((string key) => "/some/path/to/key.pub.pem");
        _settingsMock
            .Setup(o => o["MdsSettings:JwtSettings:Audience"])
            .Returns((string key) => TestConstants.TestJwtIssuer);
        _settingsMock
            .Setup(o => o["MdsSettings:JwtSettings:Issuer"])
            .Returns((string key) => TestConstants.TestJwtIssuer);
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        using var client = _factory.CreateClient();
        var adminAuthToken = await GetAdminAuthToken(client);

        // Act
        var updateResponse = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ConfigurationRequestBody
                    {
                        Internal = new ConfigurationBlock
                        {
                            IdentityUrl = "http://internal:8888",
                            NsUrl = "http://internal:8888",
                            QsUrl = "http://internal:8888",
                            FsUrl = "http://internal:8888",
                            SfUrl = "http://internal:8888",
                            SmUrl = "http://internal:8888",
                            AllowSelfSignCert = false
                        },
                        External = new ConfigurationBlock
                        {
                            IdentityUrl = "https://external:8081",
                            NsUrl = "http://external:8082",
                            QsUrl = "http://external:8083",
                            FsUrl = "http://external:8084",
                            SfUrl = "http://external:8085",
                            SmUrl = "http://external:8086",
                            AllowSelfSignCert = false
                        }
                    }
                ),
                Url = "/v1/configuration",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        var internalResponse = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Url = "/v1/configuration?scope=internal",
                HttpMethod = HttpMethod.Get,
                AllowSelfSignCert = true
            }
        );
        var externalResponse = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Url = "/v1/configuration?scope=external",
                HttpMethod = HttpMethod.Get,
                AllowSelfSignCert = true
            }
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, internalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, externalResponse.StatusCode);

        var internalBody = JsonConvert.DeserializeObject<ConfigurationResponseBody>(
            internalResponse!.Content.ReadAsStringAsync().Result
        );
        var externalBody = JsonConvert.DeserializeObject<ConfigurationResponseBody>(
            externalResponse!.Content.ReadAsStringAsync().Result
        );

        Assert.Equal("http://internal:8888", internalBody.NsUrl);
        Assert.Equal("http://internal:8888", internalBody.QsUrl);
        Assert.Equal("http://internal:8888", internalBody.FsUrl);
        Assert.Equal("http://internal:8888", internalBody.SfUrl);
        Assert.Equal("http://internal:8888", internalBody.SmUrl);
        Assert.False(internalBody.AllowSelfSignCert);
        Assert.Equal("http://external:8082", externalBody.NsUrl);
        Assert.Equal("http://external:8083", externalBody.QsUrl);
        Assert.Equal("http://external:8084", externalBody.FsUrl);
        Assert.Equal("http://external:8085", externalBody.SfUrl);
        Assert.Equal("http://external:8086", externalBody.SmUrl);
        Assert.False(externalBody.AllowSelfSignCert);
    }

    [Fact(DisplayName = "POST fails when not admin")]
    public async Task POST_fails_when_non_admin_token_provided()
    {
        // Arrange
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key"))))
            .Returns(TestConstants.TestPrivateKeyData);
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key.pub.pem"))))
            .Returns(TestConstants.TestPublicPemData);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePath"])
            .Returns((string key) => "/some/path/to/key");
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePassword"])
            .Returns((string key) => TestConstants.TestPrivateKeyPassword);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PublicPath"])
            .Returns((string key) => "/some/path/to/key.pub.pem");
        _settingsMock
            .Setup(o => o["MdsSettings:JwtSettings:Audience"])
            .Returns((string key) => TestConstants.TestJwtIssuer);
        _settingsMock
            .Setup(o => o["MdsSettings:JwtSettings:Issuer"])
            .Returns((string key) => TestConstants.TestJwtIssuer);
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        using var client = _factory.CreateClient();
        var userDetails = await UserHelpers.CreateTestUser(client);
        var authToken = await GetAuthToken(
            client,
            userDetails.AccountId,
            userDetails.UserName,
            userDetails.Password
        );

        // Act
        var updateResponse = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ConfigurationRequestBody
                    {
                        Internal = new ConfigurationBlock
                        {
                            IdentityUrl = "http://internal:8888",
                            NsUrl = "http://internal:8888",
                            QsUrl = "http://internal:8888",
                            FsUrl = "http://internal:8888",
                            SfUrl = "http://internal:8888",
                            SmUrl = "http://internal:8888",
                            AllowSelfSignCert = false
                        },
                        External = new ConfigurationBlock
                        {
                            IdentityUrl = "https://external:8081",
                            NsUrl = "http://external:8082",
                            QsUrl = "http://external:8083",
                            FsUrl = "http://external:8084",
                            SfUrl = "http://external:8085",
                            SmUrl = "http://external:8086",
                            AllowSelfSignCert = false
                        }
                    }
                ),
                Url = "/v1/configuration",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = authToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
