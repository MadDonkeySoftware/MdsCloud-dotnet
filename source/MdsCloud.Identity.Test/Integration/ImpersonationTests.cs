using System.Net;
using System.Transactions;
using Dapper;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using MdsCloud.Identity.UI.DTOs.Authentication;
using MdsCloud.Identity.UI.DTOs.Impersonation;
using MdsCloud.Identity.UI.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test.Integration;

public class ImpersonationTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityDatabaseBuilder _dbBuilder;
    private readonly IdentityWebApplicationFactory _factory;

    private readonly Mock<IFile> _fileMock = new();
    private readonly Mock<ISettings> _settingsMock = new();
    private readonly Mock<IRequestUtilities> _requestUtilitiesMock = new();

    private string _adminAuthToken = string.Empty;

    public ImpersonationTests(IdentityDatabaseBuilder dbBuilder)
    {
        _dbBuilder = dbBuilder;
        _dbBuilder.Initialize().Wait();

        _factory = new IdentityWebApplicationFactory();
        _factory.DbConnectionString = dbBuilder.TestDbConnectionString;

        var testRequestUtilities = new TestRequestUtilities(_settingsMock.Object, _fileMock.Object);
        _requestUtilitiesMock
            .Setup(o => o.GetRequestJwt(It.IsAny<string>()))
            .Returns<string>(header => testRequestUtilities.GetRequestJwt(header));

        _factory.OnConfigureServices += (_, services) =>
        {
            services.AddSingleton<IFile>(_fileMock.Object);
            services.AddSingleton<ISettings>(_settingsMock.Object);
            services.AddSingleton<IRequestUtilities>(_requestUtilitiesMock.Object);
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
        if (string.IsNullOrEmpty(_adminAuthToken))
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

            _adminAuthToken = body.Token;
        }

        return _adminAuthToken;
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

    [Fact(DisplayName = "POST when all parameters valid returns properly configured JWT")]
    public async Task POST_when_all_valid_returns_proper_JWT()
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

        // Act
        var adminAuthToken = await GetAdminAuthToken(client);
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody() { AccountId = userDetails.AccountId }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);

        var body = JsonConvert.DeserializeObject<AuthenticationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        var jwtToken = TestHttpRequestFactory.GetJwtToken(body!.Token);
        var accountId = jwtToken.Claims.First(c => c.Type == "accountId").Value;
        var userId = jwtToken.Claims.First(c => c.Type == "userId").Value;
        var impersonatingFrom = jwtToken.Claims.First(c => c.Type == "impersonatingFrom").Value;
        var impersonatedBy = jwtToken.Claims.First(c => c.Type == "impersonatedBy").Value;
        Assert.Equal(userDetails.AccountId, accountId);
        Assert.Equal(userDetails.UserName, userId);
        Assert.Equal("1", impersonatingFrom);
        Assert.Equal("mdsCloud", impersonatedBy);
    }

    [Fact(
        DisplayName = "POST when non-impersonation-enabled account makes request returns forbidden"
    )]
    public async Task POST_when_non_impersonation_enabled_account_makes_request_returns_forbidden()
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
        var userDetails1 = await UserHelpers.CreateTestUser(client);
        var userDetails2 = await UserHelpers.CreateTestUser(client);

        // Act
        var adminAuthToken = await GetAuthToken(
            client,
            userDetails1.AccountId,
            userDetails1.UserName,
            userDetails1.Password
        );
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody() { AccountId = userDetails2.AccountId }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response?.StatusCode);
        Assert.Equal("", response!.Content.ReadAsStringAsync().Result);
    }

    [Fact(DisplayName = "POST when requesting account that does not exist returns bad request")]
    public async Task POST_When_requesting_account_that_does_not_exist_returns_bad_request()
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

        // Act
        var adminAuthToken = await GetAdminAuthToken(client);
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody() { AccountId = "9999" }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        Assert.Contains(
            "Could not find account, user, or insufficient privilege to impersonate",
            response?.Content.ReadAsStringAsync().Result
        );
    }

    [Fact(DisplayName = "POST when requesting account that does inactive returns bad request")]
    public async Task POST_When_requesting_account_that_is_inactive_returns_bad_request()
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
        var connFactory = new ConnectionFactory(_dbBuilder.TestDbConnectionString);
        using (var transaction = new TransactionScope())
        {
            connFactory.WithConnection(
                conn =>
                    conn.Execute(
                        "UPDATE account SET is_active = false WHERE id = @id",
                        new { id = long.Parse(userDetails.AccountId) }
                    )
            );
            transaction.Complete();
        }

        // Act
        var adminAuthToken = await GetAdminAuthToken(client);
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody() { AccountId = userDetails.AccountId }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        Assert.Contains(
            "Could not find account, user, or insufficient privilege to impersonate",
            response?.Content.ReadAsStringAsync().Result
        );
    }

    [Fact(DisplayName = "POST when requesting user does not exist returns bad request")]
    public async Task POST_When_requesting_user_that_does_not_exist_returns_bad_request()
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

        // Act
        var adminAuthToken = await GetAdminAuthToken(client);
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody()
                    {
                        AccountId = userDetails.AccountId,
                        UserId = "DOES_NOT_EXIST"
                    }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        Assert.Contains(
            "Could not find account, user, or insufficient privilege to impersonate",
            response?.Content.ReadAsStringAsync().Result
        );
    }

    [Fact(DisplayName = "POST when requesting user that is inactive returns bad request")]
    public async Task POST_When_requesting_user_that_is_inactive_returns_bad_request()
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
        var connFactory = new ConnectionFactory(_dbBuilder.TestDbConnectionString);
        using (var transaction = new TransactionScope())
        {
            connFactory.WithConnection(
                conn =>
                    conn.Execute(
                        "UPDATE \"user\" SET is_active = false WHERE id = @id",
                        new { id = userDetails.UserName }
                    )
            );
            transaction.Complete();
        }

        // Act
        var adminAuthToken = await GetAdminAuthToken(client);
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new ImpersonationRequestBody() { AccountId = userDetails.AccountId }
                ),
                Url = "/v1/impersonate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = adminAuthToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        Assert.Contains(
            "Could not find account, user, or insufficient privilege to impersonate",
            response?.Content.ReadAsStringAsync().Result
        );
    }
}
