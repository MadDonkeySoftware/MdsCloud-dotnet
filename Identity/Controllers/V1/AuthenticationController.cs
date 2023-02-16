using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Identity.DTOs;
using Identity.DTOs.Authentication;
using Identity.Repo;
using Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.Controllers.V1;

[Route("v1/authenticate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IdentityContext _context;
    private readonly IConfiguration _configuration;
    private readonly IRequestUtilities _requestUtilities;

    public AuthenticationController(
        ILogger<AuthenticationController> logger,
        IdentityContext context,
        IConfiguration configuration,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _requestUtilities = requestUtilities;
    }

    /// <summary>
    /// </summary>
    /// <param name="reason">The internal log message to emit</param>
    /// <returns></returns>
    private BadRequestObjectResult FailRequest(string reason)
    {
        _logger.Log(LogLevel.Debug, "{}", reason);
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
        ulong parsedAccountId = ulong.TryParse(body.AccountId, out parsedAccountId)
            ? parsedAccountId
            : 0;
        var account = _context.Accounts.FirstOrDefault(e => e.Id == parsedAccountId);
        var user = _context.Users.FirstOrDefault(e => e.Id == body.UserId);

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
            _configuration["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = System.IO.File.ReadAllText(
            _configuration["MdsSettings:Secrets:PrivatePath"] ?? ""
        );
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            _configuration["MdsSettings:Secrets:PrivatePassword"] ?? ""
        );
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        )
        {
            CryptoProviderFactory = new CryptoProviderFactory() { CacheSignatureProviders = false },
        };
        var utcNow = DateTime.UtcNow;
        var tokenDescriptor = new JwtSecurityToken(
            new JwtHeader(signingCredentials),
            new JwtPayload(
                audience: _configuration["MdsSettings:JwtSettings:Audience"],
                issuer: _configuration["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("AccountId", account.Id.ToString()),
                    new("UserId", user.Id),
                    new("FriendlyName", user.FriendlyName),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );
        return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor) });
    }
}
