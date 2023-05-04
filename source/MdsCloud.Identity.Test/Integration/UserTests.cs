using System.Net;
using Dapper;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using MdsCloud.Identity.UI.Controllers.V1;
using MdsCloud.Identity.UI.DTOs.Authentication;
using MdsCloud.Identity.UI.DTOs.User;
using MdsCloud.Identity.UI.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test.Integration;

public class UserTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityDatabaseBuilder _dbBuilder;
    private readonly IdentityWebApplicationFactory _factory;

    private readonly Mock<IFile> _fileMock = new();
    private readonly Mock<ISettings> _settingsMock = new();
    private readonly Mock<IRequestUtilities> _requestUtilitiesMock = new();
    private readonly Mock<ILogger<UserController>> _logger = new();

    private string _authToken = string.Empty;

    public UserTests(IdentityDatabaseBuilder dbBuilder)
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
        if (string.IsNullOrEmpty(_authToken))
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

            _authToken = body.Token;
        }

        return _authToken;
    }

    [Fact(DisplayName = "POST when all parameters valid updates the user accordingly")]
    public async Task POST_when_all_valid_updates_user_accordingly()
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
        var beforePassword = connFactory.WithConnection(
            conn =>
                conn.ExecuteScalar<string>(
                    "SELECT password FROM \"user\" WHERE id = @id",
                    new { id = userDetails.UserName }
                )
        );

        // Act
        var authToken = await GetAuthToken(
            client,
            userDetails.AccountId,
            userDetails.UserName,
            userDetails.Password
        );
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new UpdateUserRequestBody
                    {
                        Email = "new@testing.local",
                        FriendlyName = "New FriendlyName",
                        NewPassword = "updatedPassword",
                        OldPassword = userDetails.Password
                    }
                ),
                Url = "/v1/updateUser",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = authToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response?.StatusCode);
        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Equal(string.Empty, body);
        connFactory.WithConnection(conn =>
        {
            var user = conn.QueryFirst<User>(
                "SELECT * FROM \"user\" WHERE id = @id",
                new { id = userDetails.UserName }
            );
            var afterPassword = user.Password;

            Assert.NotEqual(beforePassword, afterPassword);
            Assert.Equal("new@testing.local", user.Email);
            Assert.Equal("New FriendlyName", user.FriendlyName);

            return 0;
        });
    }

    [Fact(DisplayName = "POST when old password incorrect fails updating the user")]
    public async Task POST_when_old_password_incorrect_fails()
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
        var authToken = await GetAuthToken(
            client,
            userDetails.AccountId,
            userDetails.UserName,
            userDetails.Password
        );
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new UpdateUserRequestBody
                    {
                        Email = "new@testing.local",
                        FriendlyName = "New FriendlyName",
                        NewPassword = "updatedPassword",
                        OldPassword = $"{userDetails.Password}___"
                    }
                ),
                Url = "/v1/updateUser",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = authToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }

    [Fact(DisplayName = "POST when nothing to update fails accordingly")]
    public async Task POST_when_nothing_to_update_fails()
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
        var authToken = await GetAuthToken(
            client,
            userDetails.AccountId,
            userDetails.UserName,
            userDetails.Password
        );
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new UpdateUserRequestBody
                    {
                        NewPassword = userDetails.Password,
                        OldPassword = userDetails.Password
                    }
                ),
                Url = "/v1/updateUser",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
                AuthToken = authToken,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);
        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Could not find account, user, or passwords did not match", body);
    }
}
