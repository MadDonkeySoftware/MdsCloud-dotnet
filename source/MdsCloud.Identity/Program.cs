using System.Security.Cryptography;
using System.Transactions;
using Dapper;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.Domain;
using MdsCloud.Common.API.Logging;
using MdsCloud.Common.API.Middleware;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Business.Services;
using MdsCloud.Identity.Domain.Lookups;
using MdsCloud.Identity.Infrastructure.Repositories;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.UI.Authentication;
using MdsCloud.Identity.UI.Authorization;
using MdsCloud.Identity.UI.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;

Task InitializeSystemData(WebApplication? app, IConfiguration config, int attempts = 0)
{
    try
    {
        if (app == null)
        {
            throw new NullReferenceException("app");
        }

        using var scope = app.Services.CreateScope();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var landscapeUrlRepository =
            scope.ServiceProvider.GetRequiredService<ILandscapeUrlRepository>();

        var logger = app.Logger;
        using var transaction = new TransactionScope();

        var systemInitialized = accountRepository.AccountWithNameExists("System");
        if (!systemInitialized)
        {
            var envPass = Environment.GetEnvironmentVariable("MDS_SYS_PASSWORD");
            var newPass = envPass ?? RandomStringGenerator.GenerateString(32); // TODO: Check config
            var userId = config["MdsSettings:systemUser"] ?? "mdsCloud";
            var account = new Account
            {
                Name = "System",
                Created = DateTime.UtcNow,
                IsActive = true,
            };
            var user = new User
            {
                Id = userId,
                Created = DateTime.Now, // TODO: Update to UTC NOW
                IsActive = true,
                IsPrimary = true,
                Email = "system@localhost",
                Password = PasswordHasher.Hash(newPass),
                ActivationCode = null,
                FriendlyName = "System",
            };
            accountRepository.SaveAccount(account);
            user.AccountId = account.Id;
            userRepository.SaveUser(user);

            var landscapeUrls = new List<LandscapeUrl>()
            {
                // External
                new(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.IdentityUrl,
                    "https://127.0.0.1:8081"
                ),
                new(LandscapeUrlScopes.External, LandscapeUrlKeys.NsUrl, "http://127.0.0.1:8082"),
                new(LandscapeUrlScopes.External, LandscapeUrlKeys.QsUrl, "http://127.0.0.1:8083"),
                new(LandscapeUrlScopes.External, LandscapeUrlKeys.FsUrl, "http://127.0.0.1:8084"),
                new(LandscapeUrlScopes.External, LandscapeUrlKeys.SfUrl, "http://127.0.0.1:8085"),
                new(LandscapeUrlScopes.External, LandscapeUrlKeys.SmUrl, "http://127.0.0.1:8086"),
                new(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.AllowSelfSignCert,
                    true.ToString()
                ),
                // Internal
                new(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.IdentityUrl,
                    "http://mds-identity:8888"
                ),
                new(LandscapeUrlScopes.Internal, LandscapeUrlKeys.NsUrl, "http://mds-ns:8888"),
                new(LandscapeUrlScopes.Internal, LandscapeUrlKeys.QsUrl, "http://mds-qs:8888"),
                new(LandscapeUrlScopes.Internal, LandscapeUrlKeys.FsUrl, "http://mds-fs:8888"),
                new(LandscapeUrlScopes.Internal, LandscapeUrlKeys.SfUrl, "http://mds-sf:8888"),
                new(LandscapeUrlScopes.Internal, LandscapeUrlKeys.SmUrl, "http://mds-sm:8888"),
                new(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.AllowSelfSignCert,
                    true.ToString()
                )
            };

            foreach (var url in landscapeUrls)
            {
                landscapeUrlRepository.SaveLandscapeUrl(url);
            }

            try
            {
                transaction.Complete();
                logger.Log(LogLevel.Information, "System user created");

                var connFactory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
                connFactory.WithConnection(
                    conn => conn.Execute($"ALTER SEQUENCE Account_PK_seq RESTART WITH 1001")
                );

                if (envPass == null)
                {
                    logger.Log(
                        LogLevel.Warning,
                        "System user will be created with random password: \"{newPass}\". Be use to change the default password using the \"updateUser\" endpoint!",
                        newPass
                    );
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex, "Failed to create system user");
            }
        }
        else
        {
            logger.Log(LogLevel.Information, "System already initialized");
        }
    }
    catch (Exception)
    {
        if (attempts >= 5)
            throw;

        Thread.Sleep((int)Math.Pow(2, attempts) * 1000);
        return InitializeSystemData(app, config, attempts + 1);
    }

    return Task.CompletedTask;
}

var configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, false)
    .AddJsonFile("appsettings.Development.json", true, false)
    .AddJsonFile("appsettings.Production.json", true, false)
    .Build();
var settingsRoot = new Settings(configRoot);

void InitializeSystemServices(WebApplicationBuilder builder)
{
    // Infrastructure style items
    builder.Services.AddSingleton<ILogger>(
        _ =>
            LoggerFactory
                .Create(config =>
                {
                    config.AddConsole();
                })
                .CreateLogger<ILogger>()
    );
    builder.Services.AddSingleton<ISettings>(settingsRoot);
    builder.Services.AddSingleton<IFile, FileWrapper>();
    builder.Services.AddSingleton<IRequestUtilities, RequestUtilities>();
    builder.Services.AddScoped<IConnectionFactory>(
        provider => new ConnectionFactory(configRoot.GetConnectionString("DBConnection"))
    );
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddScoped<ILandscapeUrlRepository, LandscapeUrlRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // Services
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
    builder.Services.AddScoped<IUserService, UserService>();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(sg =>
{
    sg.EnableAnnotations();
});

InitializeSystemServices(builder);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(config =>
{
    config.AddMdsLogStashLogger(config =>
    {
        // TODO: Get these values reading from config directly.
        config.ServiceName = configRoot["Logging:MdsLogger:ServiceName"];
        config.LogStashUrl = configRoot["Logging:MdsLogger:LogStashUrl"];
        config.Enabled =
            bool.TryParse(configRoot["Logging:MdsLogger:Enabled"], out var loggerEnabled)
            && loggerEnabled;
    });
});

builder.Services.AddAuthorization(PolicyManager.ConfigureAuthorizationPolicies);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var logger = LoggerFactory
            .Create(config =>
            {
                config.AddConsole();
            })
            .CreateLogger("Program");

        var privateKeyBytes = File.ReadAllText(configRoot["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            configRoot["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );

        options.TokenValidationParameters = SecurityHelpers.GetJwtValidationParameters(
            settingsRoot,
            rsa
        );

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                logger.Log(
                    LogLevel.Debug,
                    "Auth Header: {}",
                    context.Request.Headers.Authorization.ToString()
                );

                var authHeader = context.Request.Headers.Authorization.ToString();
                const string prefix = "bearer: ";
                context.Token = authHeader.ToLowerInvariant().StartsWith(prefix)
                    ? authHeader.Substring(prefix.Length)
                    : authHeader;
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<JwtKeyAuthenticationOptions, JwtAuthenticationHandler>(
        JwtAuthentication.AuthenticationScheme,
        (_) => { }
    );

var app = builder.Build();

var sysUserSetupTask = Task.Run(() => InitializeSystemData(app, configRoot));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Middlewares
// app.ValidateTokenMiddleware();
app.UseCrossSystemTraceId();

app.UseExceptionHandler("/error");

app.MapControllers();

Task.WaitAll(sysUserSetupTask);
app.Run();
