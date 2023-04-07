using System.Net;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.DTOs.PublicSignature;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Test.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace MdsCloud.Identity.Test.Integration;

public class PublicSignatureTests : IDisposable, IClassFixture<IdentityDatabaseBuilder>
{
    private readonly IdentityWebApplicationFactory _factory;

    private readonly Mock<IFile> _fileMock = new();
    private readonly Mock<ISettings> _settingsMock = new();

    public PublicSignatureTests(IdentityDatabaseBuilder dbBuilder)
    {
        dbBuilder.Initialize().Wait();
        _factory = new IdentityWebApplicationFactory();
        _factory.DbConnectionString = dbBuilder.TestDbConnectionString;
        _factory.OnConfigureServices += (_, services) =>
        {
            services.AddSingleton<IFile>(_fileMock.Object);
            services.AddSingleton<ISettings>(_settingsMock.Object);
        };
    }

    public void Dispose()
    {
        // Dispose runs after each test.
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "GET public signature works when no auth provided")]
    public async Task GET_public_signature_works_when_no_auth_provided()
    {
        // Arrange
        _fileMock
            .Setup(o => o.ReadAllText(It.Is<string>(x => x.EndsWith("key.pub.pem"))))
            .Returns(TestConstants.TestPublicPemData);
        _settingsMock
            .Setup(o => o["MdsSettings:Secrets:PublicPath"])
            .Returns((string key) => "/some/path/to/key.pub.pem");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/v1/publicSignature");

        // Assert
        var trash = response.Content.ReadAsStringAsync().Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonConvert.DeserializeObject<PublicSignatureResponseBody>(
            response.Content.ReadAsStringAsync().Result
        );
        Assert.Equal(TestConstants.TestPublicPemData, body?.Signature);
    }
}
