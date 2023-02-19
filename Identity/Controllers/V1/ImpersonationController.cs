using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Identity.Authorization;
using Identity.Domain;
using Identity.DTOs;
using Identity.DTOs.Impersonation;
using Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.Controllers.V1;

[Route("v1/impersonate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ImpersonationController : MdsControllerBase
{
    public ImpersonationController(
        ILogger<ImpersonationController> logger,
        ISessionFactory sessionFactory,
        IConfiguration configuration,
        IRequestUtilities requestUtilities
    )
        : base(logger, sessionFactory, configuration, requestUtilities) { }

    [Authorize(Policy = Policies.Impersonator)]
    [HttpPost(Name = "Impersonate")]
    [ProducesResponseType(typeof(ImpersonationResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Creates a new account and primary user in the system",
        Summary = "New account registration",
        Tags = new[] { "Authentication" }
    )]
    public IActionResult Post(
        [FromHeader(Name = "Authorization")] string authorization,
        [FromBody] ImpersonationRequestBody body
    )
    {
        const string errorMessage =
            "Could not find account, user, or insufficient privilege to impersonate";
        using var session = SessionFactory.OpenSession();
        var jwt = RequestUtilities.GetRequestJwt(authorization);
        var accountId = jwt.Claims.First(c => c.Type == "AccountId").Value;
        var userId = jwt.Claims.First(c => c.Type == "UserId").Value;
        long impersonatorAccountId = long.TryParse(accountId, out impersonatorAccountId)
            ? impersonatorAccountId
            : -1;

        if (impersonatorAccountId != 1 && accountId != body.AccountId)
        {
            return FailRequest(
                "Non-system account attempted to impersonate user outside of account",
                errorMessage
            );
        }

        long parsedAccountId = long.TryParse(body.AccountId, out parsedAccountId)
            ? parsedAccountId
            : -1;
        var account = session.Query<Account>().FirstOrDefault(e => e.Id == parsedAccountId);
        if (account == null)
        {
            return FailRequest("No such account found", errorMessage);
        }
        if (!account.IsActive)
        {
            return FailRequest("Account is not active", errorMessage);
        }

        var userIdToSearchFor = body.UserId ?? account.Users.First(u => u.IsPrimary).Id;
        var user = session.Query<User>().FirstOrDefault(e => e.Id == userIdToSearchFor);

        if (user == null)
        {
            return FailRequest("No such user found", errorMessage);
        }

        if (!user.IsActive)
        {
            return FailRequest("User not active", errorMessage);
        }

        double parsedLifespanMinutes = double.TryParse(
            Configuration["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = System.IO.File.ReadAllText(
            Configuration["MdsSettings:Secrets:PrivatePath"] ?? ""
        );
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            Configuration["MdsSettings:Secrets:PrivatePassword"] ?? ""
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
                audience: Configuration["MdsSettings:JwtSettings:Audience"],
                issuer: Configuration["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("AccountId", account.Id.ToString()),
                    new("UserId", user.Id),
                    new("FriendlyName", user.FriendlyName),
                    new("ImpersonatedBy", userId),
                    new("ImpersonatingFrom", accountId),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );
        return Ok(
            new ImpersonationResponseBody
            {
                Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor)
            }
        );
    }
}
