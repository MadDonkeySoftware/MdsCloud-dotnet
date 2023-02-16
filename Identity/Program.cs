using System.Security.Cryptography;
using Identity.Authentication;
using Identity.Authorization;
using Identity.Domain;
using Identity.Middlewares;
using Identity.Repo;
using Identity.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

async Task SetupSystemUser(WebApplication? app, IConfiguration config)
{
    if (app == null)
    {
        throw new NullReferenceException("app");
    }

    using var scope = app.Services.CreateScope();
    await using var ctx = scope.ServiceProvider.GetRequiredService<IdentityContext>();

    var logger = app.Logger;
    var existsInDb = ctx.Accounts.FirstOrDefault(e => e.Name == "System") != null;
    if (!existsInDb)
    {
        var envPass = Environment.GetEnvironmentVariable("MDS_SYS_PASSWORD");
        var newPass = envPass ?? RandomStringGenerator.GenerateString(32); // TODO: Check config
        var userId = config["MdsSettings:systemUser"] ?? "mdsCloud";
        var account = new Account()
        {
            Id = 1,
            Name = "System",
            Created = DateTime.Now,
            IsActive = true,
        };
        var user = new User()
        {
            Id = userId,
            Created = DateTime.Now,
            IsActive = true,
            IsPrimary = true,
            Email = "system@localhost",
            Password = PasswordHasher.Hash(newPass),
            ActivationCode = null,
            FriendlyName = "System",
        };
        account.Users.Add(user);
        ctx.Accounts.Add(account);
        try
        {
            ctx.SaveChanges();
            logger.Log(LogLevel.Information, "System user created");

            ctx.Database.ExecuteSql($"ALTER TABLE Accounts AUTO_INCREMENT=1001");
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

var configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile("appsettings.Development.json", true, true)
    .Build();
var requestUtilities = new RequestUtilities(configRoot);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(sg =>
{
    sg.EnableAnnotations();
});

builder.Services.AddSingleton<IConfiguration>(configRoot);
builder.Services.AddSingleton<IRequestUtilities>(requestUtilities);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<IdentityContext>(opt =>
{
    opt.UseMySql(
        builder.Configuration.GetConnectionString("DBConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DBConnection")),
        b => b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    );
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Services.AddAuthorization(PolicyManager.ConfigureAuthorizationPolicies);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var privateKeyBytes = File.ReadAllText(configRoot["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            configRoot["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );
        var logger = LoggerFactory
            .Create(config =>
            {
                config.AddConsole();
            })
            .CreateLogger("Program");

        options.TokenValidationParameters = SecurityHelpers.GetJwtValidationParameters(
            configRoot,
            rsa
        );

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                logger.Log(
                    LogLevel.Information,
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

var sysUserSetupTask = Task.Run(() => SetupSystemUser(app, configRoot));

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
