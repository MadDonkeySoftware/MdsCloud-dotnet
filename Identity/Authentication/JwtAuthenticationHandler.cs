using System.Security.Principal;
using System.Text.Encodings.Web;
using Identity.Authorization;
using Identity.Repo;
using Identity.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Identity.Authentication;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IRequestUtilities _requestUtilities;
    private readonly IdentityContext _identityContext;

    public JwtAuthenticationHandler(
        IConfiguration configuration,
        IRequestUtilities requestUtilities,
        IdentityContext identityContext,
        IOptionsMonitor<JwtKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
        _requestUtilities = requestUtilities;
        _identityContext = identityContext;
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

            var accountId = jwtValidatedToken.Claims.First(c => c.Type == "AccountId").Value;
            var userId = jwtValidatedToken.Claims.First(c => c.Type == "UserId").Value;
            var friendlyName = jwtValidatedToken.Claims.First(c => c.Type == "FriendlyName").Value;

            var roles = new List<string> { Roles.User };
            var user = _identityContext.Users.First(u => u.Id == userId);
            await _identityContext.Entry(user).Reference(u => u.Account).LoadAsync();

            if (!user.IsActive)
            {
                return AuthenticateResult.Fail("User is not active");
            }
            if (!user.Account.IsActive)
            {
                return AuthenticateResult.Fail("Account is not active");
            }

            if (user.IsPrimary)
            {
                roles.Add(Roles.PrimaryUser);
            }
            if (user.Account.Id == 1)
            {
                roles.Add(Roles.SystemAccount);
            }

            var identity = new GenericIdentity($"{accountId}:{userId}");
            var principal = new GenericPrincipal(identity, roles.ToArray());

            var ticket = new AuthenticationTicket(
                principal,
                new AuthenticationProperties
                {
                    Items =
                    {
                        { "AccountId", accountId },
                        { "UserId", userId },
                        { "FriendlyName", friendlyName }
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
