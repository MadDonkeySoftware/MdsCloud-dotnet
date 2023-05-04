using System.Net;
using System.Transactions;
using Dapper;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using MdsCloud.Identity.UI.DTOs.Registration;
using MdsCloud.Identity.UI.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Npgsql;

namespace MdsCloud.Identity.Test.Integration;

public class RegistrationTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityDatabaseBuilder _dbBuilder;
    private readonly IdentityWebApplicationFactory _factory;

    private readonly Mock<ISettings> _settingsMock = new();
    private readonly Mock<IRequestUtilities> _requestUtilitiesMock = new();

    public RegistrationTests(IdentityDatabaseBuilder dbBuilder)
    {
        _dbBuilder = dbBuilder;
        _dbBuilder.Initialize().Wait();

        _factory = new IdentityWebApplicationFactory();
        _factory.DbConnectionString = dbBuilder.TestDbConnectionString;

        _factory.OnConfigureServices += (_, services) =>
        {
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

    [Fact(DisplayName = "POST when all parameters valid and user activation true")]
    public async Task POST_when_all_valid_and_user_activation_required()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "False");
        using var client = _factory.CreateClient();

        var testUsername = $"User_{RandomStringGenerator.GenerateString(8)}";
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var friendlyName = $"Test {testUsername}";
        var accountName = $"Account for {testUsername}";

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Email = testEmail,
                        Password = testPassword,
                        FriendlyName = friendlyName,
                        AccountName = accountName,
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.Created, response?.StatusCode);

        var body = JsonConvert.DeserializeObject<RegistrationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        Assert.NotEmpty(body!.AccountId!);

        // var user = session.Query<User>().First(e => e.Id == testUsername);
        await using var dbConn = new NpgsqlConnection(_dbBuilder.TestDbConnectionString);
        dbConn.Open();
        var user = dbConn.QueryFirst<User>(
            "SELECT * FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );

        Assert.False(user.IsActive);
        Assert.NotNull(user.ActivationCode);
    }

    [Fact(DisplayName = "POST when all parameters valid and user activation bypassed")]
    public async Task POST_when_all_valid_and_user_activation_bypassed()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        using var client = _factory.CreateClient();

        var testUsername = $"User_{RandomStringGenerator.GenerateString(8)}";
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var friendlyName = $"Test {testUsername}";
        var accountName = $"Account for {testUsername}";

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Email = testEmail,
                        Password = testPassword,
                        FriendlyName = friendlyName,
                        AccountName = accountName,
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.Created, response?.StatusCode);

        var body = JsonConvert.DeserializeObject<RegistrationResponseBody>(
            response!.Content.ReadAsStringAsync().Result
        );
        Assert.NotEmpty(body!.AccountId!);

        await using var dbConn = new NpgsqlConnection(_dbBuilder.TestDbConnectionString);
        dbConn.Open();
        var foo = dbConn.QueryFirst<dynamic>(
            "SELECT * FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );
        var user = dbConn.QueryFirst<User>(
            "SELECT * FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );

        Assert.True(user.IsActive);
        Assert.Null(user.ActivationCode);
    }

    [Fact(DisplayName = "POST Fails when userId already in system")]
    public async Task POST_fails_when_user_id_already_in_system()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
        using var client = _factory.CreateClient();

        var existingUserDetails = await UserHelpers.CreateTestUser(client);

        var testUsername = existingUserDetails.UserName;
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var friendlyName = $"Test {RandomStringGenerator.GenerateString(8)}";
        var accountName = $"Account for {RandomStringGenerator.GenerateString(8)}";

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Email = testEmail,
                        Password = testPassword,
                        FriendlyName = friendlyName,
                        AccountName = accountName,
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Invalid accountName or userName", body);

        await using var dbConn = new NpgsqlConnection(_dbBuilder.TestDbConnectionString);
        dbConn.Open();
        var existingUserCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );
        var existingAccountCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM account WHERE name = @name",
            new { name = accountName }
        );

        Assert.Equal(1, existingUserCount);
        Assert.Equal(0, existingAccountCount);
    }

    [Fact(DisplayName = "POST Fails when account name already in system")]
    public async Task POST_fails_when_account_name_already_in_system()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
        using var client = _factory.CreateClient();

        var existingUserDetails = await UserHelpers.CreateTestUser(client);

        var testUsername = $"User_{RandomStringGenerator.GenerateString(8)}";
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var friendlyName = $"Test {RandomStringGenerator.GenerateString(8)}";
        var accountName = $"Account for {existingUserDetails.UserName}";

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Email = testEmail,
                        Password = testPassword,
                        FriendlyName = friendlyName,
                        AccountName = accountName,
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("Invalid accountName or userName", body);

        using var transaction = new TransactionScope();
        await using var dbConn = new NpgsqlConnection(_dbBuilder.TestDbConnectionString);
        dbConn.Open();
        var existingUserCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );
        var existingAccountCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM account WHERE name = @name",
            new { name = accountName }
        );

        Assert.Equal(0, existingUserCount);
        Assert.Equal(1, existingAccountCount);
    }

    [Fact(DisplayName = "POST Fails when insufficient data provided in request")]
    public async Task POST_fails_when_insufficient_data_in_request()
    {
        // Arrange
        _settingsMock
            .Setup(o => o["MdsSettings:BypassUserActivation"])
            .Returns((string key) => "True");
        _requestUtilitiesMock.Setup(o => o.Delay(It.IsAny<int>())).Callback<int>(NoDelay);
        using var client = _factory.CreateClient();

        var testUsername = $"User_{RandomStringGenerator.GenerateString(8)}";
        var testPassword = $"password_{RandomStringGenerator.GenerateString(8)}";
        var testEmail = $"{RandomStringGenerator.GenerateString(8)}@no.com";
        var accountName = $"Account for {RandomStringGenerator.GenerateString(8)}";

        // Act
        var response = await TestHttpRequestFactory.MakeRequest(
            client,
            new CreateRequestArgs
            {
                Content = TestHttpRequestFactory.CreateJsonContent(
                    new RegistrationRequestBody
                    {
                        UserId = testUsername,
                        Email = testEmail,
                        Password = testPassword,
                        AccountName = accountName,
                    }
                ),
                Url = "/v1/register",
                HttpMethod = HttpMethod.Post,
                AllowSelfSignCert = true,
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response?.StatusCode);

        var body = response!.Content.ReadAsStringAsync().Result;
        Assert.Contains("The FriendlyName field is required", body);

        await using var dbConn = new NpgsqlConnection(_dbBuilder.TestDbConnectionString);
        dbConn.Open();
        var existingUserCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM \"user\" WHERE id = @id",
            new { id = testUsername }
        );
        var existingAccountCount = dbConn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM account WHERE name = @name",
            new { name = accountName }
        );

        Assert.Equal(0, existingUserCount);
        Assert.Equal(0, existingAccountCount);
    }
}
