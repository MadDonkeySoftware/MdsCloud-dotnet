using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.Authorization;
using MdsCloud.Identity.Presentation.DTOs;
using MdsCloud.Identity.Presentation.DTOs.User;
using MdsCloud.Identity.Presentation.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

[Route("v1/updateUser")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IRequestUtilities _requestUtilities;
    private readonly IUserService _userService;

    public UserController(
        ILogger<UserController> logger,
        IRequestUtilities requestUtilities,
        IUserService userService
    )
    {
        _logger = logger;
        _requestUtilities = requestUtilities;
        _userService = userService;
    }

    /// <summary>
    /// </summary>
    /// <param name="reason">The internal log message to emit</param>
    /// <returns></returns>
    private BadRequestObjectResult FailRequest(string reason)
    {
        _logger.LogWithMetadata(LogLevel.Debug, reason, this.Request.GetMdsTraceId());
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

    [Authorize(Policies.User)]
    [HttpPost(Name = "Update User")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Creates a new account and primary user in the system",
        Summary = "New account registration",
        Tags = new[] { "User" }
    )]
    public IActionResult Post(
        [FromHeader(Name = "Authorization")] string authorization,
        [FromBody] UpdateUserRequestBody body
    )
    {
        try
        {
            _userService.UpdateUserData(
                new ArgsWithTrace<UpdateUserDataArgs>
                {
                    MdsTraceId = Request.GetMdsTraceId(),
                    Data = new UpdateUserDataArgs
                    {
                        Email = body.Email,
                        FriendlyName = body.FriendlyName,
                        NewPassword = body.NewPassword,
                        OldPassword = body.OldPassword,
                        RequestingUserJwt = _requestUtilities.GetRequestJwt(authorization)
                    }
                }
            );
            return Ok();
        }
        catch (InvalidPasswordException)
        {
            return FailRequest("Old password verification failed.");
        }
        catch (ArgumentException ex)
        {
            return FailRequest(ex.Message);
        }
    }
}
