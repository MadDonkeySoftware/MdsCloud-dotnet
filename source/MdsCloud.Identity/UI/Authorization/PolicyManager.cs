using MdsCloud.Identity.UI.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace MdsCloud.Identity.UI.Authorization;

public static class PolicyManager
{
    public static void ConfigureAuthorizationPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(
            Policies.User,
            policy =>
            {
                policy.RequireRole(Roles.User);
                policy.AuthenticationSchemes.Add(JwtAuthentication.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            }
        );

        options.AddPolicy(
            Policies.Impersonator,
            policy =>
            {
                policy.RequireRole(Roles.Impersonator);
                policy.AuthenticationSchemes.Add(JwtAuthentication.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            }
        );

        options.AddPolicy(
            Policies.System,
            policy =>
            {
                policy.RequireRole(Roles.SystemAccount);
                policy.AuthenticationSchemes.Add(JwtAuthentication.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            }
        );
    }
}
