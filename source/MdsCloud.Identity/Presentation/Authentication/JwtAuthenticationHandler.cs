using System.Security.Principal;
using System.Text.Encodings.Web;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.Authorization;
using MdsCloud.Identity.Presentation.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace MdsCloud.Identity.Presentation.Authentication;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtKeyAuthenticationOptions>
{
    private readonly IRequestUtilities _requestUtilities;
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;

    public JwtAuthenticationHandler(
        IRequestUtilities requestUtilities,
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        IOptionsMonitor<JwtKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock)
    {
        _requestUtilities = requestUtilities;
        _accountRepository = accountRepository;
        _userRepository = userRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Task.Yield();

        if (!Request.Headers.TryGetValue("Authorization", out var headerAuthorization))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var jwtValidatedToken = _requestUtilities.GetRequestJwt(headerAuthorization.ToString());

            var accountId = jwtValidatedToken.Claims.First(c => c.Type == "accountId").Value;
            var userId = jwtValidatedToken.Claims.First(c => c.Type == "userId").Value;
            var friendlyName = jwtValidatedToken.Claims.First(c => c.Type == "friendlyName").Value;

            var roles = new List<string> { Roles.User };
            var user = _userRepository.GetById(userId);
            var account = _accountRepository.GetById(user.AccountId);

            if (user.AccountId.ToString() != accountId)
            {
                return AuthenticateResult.Fail(
                    "Requested user does not belong to requested account."
                );
            }
            if (!user.IsActive)
            {
                return AuthenticateResult.Fail("User is not active");
            }
            if (!account.IsActive)
            {
                return AuthenticateResult.Fail("Account is not active");
            }

            if (user.IsPrimary)
            {
                roles.Add(Roles.PrimaryUser);
            }
            if (account.Id == 1)
            {
                roles.Add(Roles.SystemAccount);
                roles.Add(Roles.Impersonator);
            }

            var identity = new GenericIdentity($"{accountId}:{userId}");
            var principal = new GenericPrincipal(identity, roles.ToArray());

            var ticket = new AuthenticationTicket(
                principal,
                new AuthenticationProperties
                {
                    Items =
                    {
                        { "accountId", accountId },
                        { "userId", userId },
                        { "friendlyName", friendlyName }
                    },
                    ExpiresUtc = jwtValidatedToken.ValidTo.ToUniversalTime(),
                },
                JwtAuthentication.AuthenticationScheme
            );

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex);
        }
    }
}
