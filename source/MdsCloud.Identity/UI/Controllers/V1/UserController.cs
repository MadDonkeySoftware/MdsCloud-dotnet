using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Exceptions;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Business.Services;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.UI.Authorization;
using MdsCloud.Identity.UI.DTOs;
using MdsCloud.Identity.UI.DTOs.User;
using MdsCloud.Identity.UI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.UI.Controllers.V1;

[Route("v1/updateUser")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly IRequestUtilities _requestUtilities;
    private readonly IUserService _userService;

    public UserController(
        ILogger<UserController> logger,
        ISessionFactory sessionFactory,
        IRequestUtilities requestUtilities,
        IUserService userService
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
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
