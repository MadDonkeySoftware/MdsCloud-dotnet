using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MdsCloud.Identity.Test.TestHelpers;

public class IdentityWebApplicationFactory : WebApplicationFactory<Domain.Account>
{
    public EventHandler<IServiceCollection>? OnConfigureServices;
    public string? DbConnectionString { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DBConnection", DbConnectionString);
        Environment.SetEnvironmentVariable(
            "MDS_SYS_PASSWORD",
            TestConstants.TestMdsSystemRootUserPassword
        );

        builder.ConfigureTestServices(services =>
        {
            OnConfigureServices?.Invoke(this, services);
        });
        base.ConfigureWebHost(builder);
    }
}
