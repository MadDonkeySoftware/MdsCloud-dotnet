using System.Net;
using System.Transactions;
using Dapper;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using MdsCloud.Identity.UI.DTOs.Authentication;
using MdsCloud.Identity.UI.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test.Integration;

public class AuthenticationTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityDatabaseBuilder _dbBuilder;
    private readonly IdentityWebApplicationFactory _factory;

    private readonly Mock<IFile> _fileMock = new();
    private readonly Mock<ISettings> _settingsMock = new();
    private readonly Mock<IRequestUtilities> _requestUtilitiesMock = new();

    public AuthenticationTests(IdentityDatabaseBuilder dbBuilder)
    {
        _dbBuilder = dbBuilder;
        _dbBuilder.Initialize().Wait();

        _factory = new IdentityWebApplicationFactory();
        _factory.DbConnectionString = dbBuilder.TestDbConnectionString;

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

    private void NoDelay(int milliseconds)
    {
        // Do nothing
    }

    [Fact(DisplayName = "POST when all parameters valid returns properly configured JWT")]
    public async Task POST_when_all_valid_returns_proper_JWT()
    {
        // Arrange
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key"))))
            .Returns(TestConstants.TestPrivateKeyData);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePath"])
            .Returns((string key) => "/some/path/to/key");
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PrivatePassword"])
            .Returns((string key) => TestConstants.TestPrivateKeyPassword);
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
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = userDetails.UserName,
                        Password = userDetails.Password,
                        AccountId = userDetails.AccountId
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
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
        Assert.Equal(userDetails.AccountId, accountId);
        Assert.Equal(userDetails.UserName, userId);
    }

    [Fact(DisplayName = "POST when invalid password request fails")]
    public async Task POST_when_invalid_password_request_fails()
    {
        // Arrange
        _factory.OnConfigureServices += (_, services) =>
        {
            services.AddSingleton<ISettings>(_settingsMock.Object);
        };
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
        using var client = _factory.CreateClient();
        var userDetails = await UserHelpers.CreateTestUser(client);

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = userDetails.UserName,
                        Password = $"{userDetails.Password}1234",
                        AccountId = userDetails.AccountId
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }

    [Fact(DisplayName = "POST when inactive user request fails")]
    public async Task POST_when_inactive_user_request_fails()
    {
        // Arrange
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
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
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = userDetails.UserName,
                        Password = userDetails.Password,
                        AccountId = userDetails.AccountId
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }

    [Fact(DisplayName = "POST when user does not exist request fails")]
    public async Task POST_when_user_does_not_exist_request_fails()
    {
        // Arrange
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
        using var client = _factory.CreateClient();

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = "nonUser",
                        Password = "1234",
                        AccountId = "1"
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }

    [Fact(DisplayName = "POST when inactive account request fails")]
    public async Task POST_when_inactive_account_request_fails()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
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
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = userDetails.UserName,
                        Password = userDetails.Password,
                        AccountId = userDetails.AccountId
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }

    [Fact(DisplayName = "POST when account does not exist request fails")]
    public async Task POST_when_account_does_not_exist_request_fails()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
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
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new AuthenticationRequestBody
                    {
                        UserId = userDetails.UserName,
                        Password = userDetails.Password,
                        AccountId = $"1234{userDetails.AccountId}"
                    }
                ),
                Url = "/v1/authenticate",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }
}
