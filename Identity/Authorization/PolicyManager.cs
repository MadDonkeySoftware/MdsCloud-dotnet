using Identity.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Authorization;

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
    }
}
