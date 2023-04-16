using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Authorization;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.Impersonation;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Controllers.V1;

[Route("v1/impersonate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ImpersonationController : MdsControllerBase
{
    private readonly IFile _file;

    public ImpersonationController(
        ILogger<ImpersonationController> logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IRequestUtilities requestUtilities,
        IFile file
    )
        : base(logger, sessionFactory, settings, requestUtilities)
    {
        _file = file;
    }

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
        var accountId = jwt.Claims.First(c => c.Type == "accountId").Value;
        var userId = jwt.Claims.First(c => c.Type == "userId").Value;
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
            Settings["MdsSettings:JwtSettings:LifespanMinutes"],
            out parsedLifespanMinutes
        )
            ? parsedLifespanMinutes
            : 60d;

        var privateKeyBytes = _file.ReadAllText(Settings["MdsSettings:Secrets:PrivatePath"] ?? "");
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(
            privateKeyBytes,
            Settings["MdsSettings:Secrets:PrivatePassword"] ?? ""
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
                audience: Settings["MdsSettings:JwtSettings:Audience"],
                issuer: Settings["MdsSettings:JwtSettings:Issuer"],
                claims: new Claim[]
                {
                    new("accountId", account.Id.ToString()),
                    new("userId", user.Id),
                    new("friendlyName", user.FriendlyName),
                    new("impersonatedBy", userId),
                    new("impersonatingFrom", accountId),
                },
                notBefore: utcNow,
                issuedAt: utcNow,
                expires: utcNow.AddMinutes(parsedLifespanMinutes)
            )
        );

        this.Logger.LogWithMetadata(
            LogLevel.Debug,
            "Impersonation successful",
            this.Request.GetMdsTraceId(),
            new
            {
                User = userId,
                Account = accountId,
                ImpersonatedUser = user.Id,
                ImpersonatedAccount = account.Id
            }
        );
        return Ok(
            new ImpersonationResponseBody
            {
                Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor)
            }
        );
    }
}
