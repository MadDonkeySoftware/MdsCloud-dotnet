using System.Security.Principal;
using System.Text.Encodings.Web;
using MdsCloud.Identity.Repo;
using MdsCloud.Identity.Authorization;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NHibernate;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace MdsCloud.Identity.Authentication;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IRequestUtilities _requestUtilities;
    private readonly ISessionFactory _sessionFactory;

    public JwtAuthenticationHandler(
        IConfiguration configuration,
        IRequestUtilities requestUtilities,
        ISessionFactory sessionFactory,
        IOptionsMonitor<JwtKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
        _requestUtilities = requestUtilities;
        _sessionFactory = sessionFactory;
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
            var session = _sessionFactory.OpenSession();
            var jwtValidatedToken = _requestUtilities.GetRequestJwt(headerAuthorization.ToString());

            var accountId = jwtValidatedToken.Claims.First(c => c.Type == "accountId").Value;
            var userId = jwtValidatedToken.Claims.First(c => c.Type == "userId").Value;
            var friendlyName = jwtValidatedToken.Claims.First(c => c.Type == "friendlyName").Value;

            var roles = new List<string> { Roles.User };
            var user = session.Query<User>().First(u => u.Id == userId);

            // NOTE: We cannot use the related Account object because postgres doesn't support MARS.
            // As an annoying work around we pull the account directly.
            var account = session.Query<Account>().First(u => u.Id == user.AccountId);
            // await _identityContext.Entry(user).Reference(u => u.Account).LoadAsync();

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
