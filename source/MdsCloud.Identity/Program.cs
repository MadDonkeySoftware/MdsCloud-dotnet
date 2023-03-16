using System.Security.Cryptography;
using MdsCloud.Identity.Authentication;
using MdsCloud.Identity.Authorization;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Repo;
using MdsCloud.Identity.Utils;
using MdsCloud.Common.API.Logging;
using MdsCloud.Common.API.Middleware;
using MdsCloud.Identity.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NHibernate;

Task InitializeSystemData(WebApplication? app, IConfiguration config, int attempts = 0)
{
    try
    {
        if (app == null)
        {
            throw new NullReferenceException("app");
        }

        using var scope = app.Services.CreateScope();
        var sessionFactory = scope.ServiceProvider.GetRequiredService<ISessionFactory>();

        var logger = app.Logger;
        using var session = sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        var systemInitialized = session.Query<Account>().Any(e => e.Name == "System");
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
            session.SaveOrUpdate(account);
            user.Account = account;
            session.SaveOrUpdate(user);

            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.IdentityUrl,
                    "https://127.0.0.1:8081"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.NsUrl,
                    "http://127.0.0.1:8082"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.QsUrl,
                    "http://127.0.0.1:8083"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.FsUrl,
                    "http://127.0.0.1:8084"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.SfUrl,
                    "http://127.0.0.1:8085"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.SmUrl,
                    "http://127.0.0.1:8086"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.External,
                    LandscapeUrlKeys.AllowSelfSignCert,
                    true.ToString()
                )
            );

            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.IdentityUrl,
                    "https://mds-identity:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.NsUrl,
                    "http://mds-ns:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.QsUrl,
                    "http://mds-qs:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.FsUrl,
                    "https://mds-fs:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.SfUrl,
                    "https://mds-sf:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.SmUrl,
                    "https://mds-sm:8888"
                )
            );
            session.SaveOrUpdate(
                new LandscapeUrl(
                    LandscapeUrlScopes.Internal,
                    LandscapeUrlKeys.AllowSelfSignCert,
                    true.ToString()
                )
            );

            try
            {
                transaction.Commit();
                logger.Log(LogLevel.Information, "System user created");

                session
                    .CreateSQLQuery($"ALTER SEQUENCE Account_PK_seq RESTART WITH 1001")
                    .ExecuteUpdate();
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

ISessionFactory CreateSessionFactory(IConfiguration config)
{
    var fluentConfig = NhibernateConfigGenerator.Generate(config);
    // fluentConfig.ExposeConfiguration((config) => new SchemaExport(config).Create(false, true));
    return fluentConfig.BuildSessionFactory();
}

var configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile("appsettings.Development.json", true, true)
    .AddJsonFile("appsettings.Production.json", true, true)
    .Build();
var requestUtilities = new RequestUtilities(configRoot);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(sg =>
{
    sg.EnableAnnotations();
});

var nhibernateSessionFactory = CreateSessionFactory(builder.Configuration);

builder.Services.AddSingleton<IConfiguration>(configRoot);
builder.Services.AddSingleton<IRequestUtilities>(requestUtilities);
builder.Services.AddSingleton<ISessionFactory>(nhibernateSessionFactory);

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
            configRoot,
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

app.MapControllers();

Task.WaitAll(sysUserSetupTask);
app.Run();
