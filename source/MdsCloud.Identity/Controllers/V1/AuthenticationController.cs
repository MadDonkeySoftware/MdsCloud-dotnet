using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.Authentication;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Controllers.V1;

[Route("v1/authenticate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly ISettings _settings;
    private readonly IRequestUtilities _requestUtilities;
    private readonly IFile _file;

    public AuthenticationController(
        ILogger<AuthenticationController> logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IRequestUtilities requestUtilities,
        IFile file
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _settings = settings;
        _requestUtilities = requestUtilities;
        _file = file;
    }

    /// <summary>
    /// </summary>
    /// <param name="reason">The internal log message to emit</param>
    /// <returns></returns>
    private BadRequestObjectResult FailRequest(string reason)
    {
        _logger.LogWithMetadata(
            LogLevel.Debug,
            reason,
            new Dictionary<string, dynamic>
            {
                { LoggingConstants.TraceLogKey, this.Request.GetMdsTraceId() }
            }
        );
        _requestUtilities.Delay(10000);
        return BadRequest(
            new BadRequestResponse(
                new Dictionary<string, string[]>
                {
                    {
                        "Message",
                        new[] { "Could not find account, user, or passwords did not match" }
                    }
                }
            )
        );
    }

    [HttpPost(Name = "Authenticate")]
    [ProducesResponseType(typeof(AuthenticationResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Creates a new account and primary user in the system",
        Summary = "New account registration",
        Tags = new[] { "Authentication" }
    )]
    public IActionResult Post([FromBody] AuthenticationRequestBody body)
    {
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        long parsedAccountId = long.TryParse(body.AccountId, out parsedAccountId)
            ? parsedAccountId
            : 0;
        var account = session.Query<Account>().FirstOrDefault(e => e.Id == parsedAccountId);
        var user = session.Query<User>().FirstOrDefault(e => e.Id == body.UserId);

        if (account == null)
        {
            return FailRequest("No such account found");
        }

        if (!account.IsActive)
        {
            return FailRequest("Account is not active");
        }

        if (user == null)
        {
            return FailRequest("No such user found");
        }

        if (!user.IsActive)
        {
            return FailRequest("User not active");
        }

        if (!PasswordHasher.Verify(body.Password, user.Password))
        {
            return FailRequest("Invalid password");
        }

        double parsedLifespanMinutes = double.TryParse(
            _settings["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = _file.ReadAllText(_settings["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            _settings["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        )
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };
        var utcNow = DateTime.UtcNow;
        var tokenDescriptor = new JwtSecurityToken(
            new JwtHeader(signingCredentials),
            new JwtPayload(
                audience: _settings["MdsSettings:JwtSettings:Audience"],
                issuer: _settings["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("accountId", account.Id.ToString()),
                    new("userId", user.Id),
                    new("friendlyName", user.FriendlyName),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );
        user.LastActivity = DateTime.UtcNow;
        session.SaveOrUpdate(user);
        transaction.Commit();

        _logger.LogWithMetadata(
            LogLevel.Debug,
            $"Successfully authenticated user {user.Id}",
            new Dictionary<string, dynamic>
            {
                { LoggingConstants.TraceLogKey, this.Request.GetMdsTraceId() }
            }
        );

        return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor) });
    }
}
