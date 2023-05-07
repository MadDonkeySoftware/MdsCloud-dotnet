using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.DTOs;
using MdsCloud.Identity.Presentation.DTOs.Authentication;
using MdsCloud.Identity.Presentation.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

[Route("v1/authenticate")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthenticationController : MdsControllerBase<AuthenticationController>
{
    private readonly ITokenService _tokenService;

    public AuthenticationController(
        ILogger<AuthenticationController> logger,
        IRequestUtilities requestUtilities,
        ITokenService tokenService
    )
        : base(logger, requestUtilities)
    {
        _tokenService = tokenService;
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
        const string reason = "Could not find account, user, or passwords did not match";
        const int delay = 10000;
        try
        {
            var token = _tokenService.GenerateUserToken(
                new ArgsWithTrace<GenerateUserTokenArgs>
                {
                    MdsTraceId = Request.GetMdsTraceId(),
                    Data = body
                }
            );
            return Ok(new { Token = token });
        }
        catch (AccountDoesNotExistException)
        {
            return FailRequest("No such account found", reason, delay);
        }
        catch (AccountInactiveException)
        {
            return FailRequest("Account is not active", reason, delay);
        }
        catch (UserDoesNotExistException)
        {
            return FailRequest("No such user found", reason, delay);
        }
        catch (UserInactiveException)
        {
            return FailRequest("User not active", reason, delay);
        }
        catch (InvalidPasswordException)
        {
            return FailRequest("Invalid password", reason, delay);
        }
    }
}
