using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.Authorization;
using MdsCloud.Identity.Presentation.DTOs;
using MdsCloud.Identity.Presentation.DTOs.Impersonation;
using MdsCloud.Identity.Presentation.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

[Route("v1/impersonate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ImpersonationController : MdsControllerBase<ImpersonationController>
{
    private readonly ITokenService _tokenService;

    public ImpersonationController(
        ILogger<ImpersonationController> logger,
        IRequestUtilities requestUtilities,
        ITokenService tokenService
    )
        : base(logger, requestUtilities)
    {
        _tokenService = tokenService;
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
        try
        {
            var token = _tokenService.GenerateImpersonationToken(
                new ArgsWithTrace<GenerateImpersonationTokenArgs>
                {
                    MdsTraceId = Request.GetMdsTraceId(),
                    Data = new GenerateImpersonationTokenArgs
                    {
                        RequestingUserJwt = RequestUtilities.GetRequestJwt(authorization),
                        AccountId = body.AccountId,
                        UserId = body.UserId,
                    }
                }
            );
            return Ok(new ImpersonationResponseBody { Token = token });
        }
        catch (AccountDoesNotExistException)
        {
            return FailRequest("No such account found", errorMessage);
        }
        catch (AccountInactiveException)
        {
            return FailRequest("Account is not active", errorMessage);
        }
        catch (UserDoesNotExistException)
        {
            return FailRequest("No such user found", errorMessage);
        }
        catch (UserInactiveException)
        {
            return FailRequest("User not active", errorMessage);
        }
    }
}
