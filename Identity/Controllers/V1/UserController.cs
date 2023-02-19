using Identity.Authorization;
using Identity.Domain;
using Identity.DTOs;
using Identity.DTOs.User;
using Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.Controllers.V1;

[Route("v1/updateUser")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly IRequestUtilities _requestUtilities;

    public UserController(
        ILogger<UserController> logger,
        ISessionFactory sessionFactory,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _requestUtilities = requestUtilities;
    }

    /// <summary>
    /// </summary>
    /// <param name="reason">The internal log message to emit</param>
    /// <returns></returns>
    private BadRequestObjectResult FailRequest(string reason)
    {
        _logger.Log(LogLevel.Debug, "{}", reason);
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
        using var session = _sessionFactory.OpenSession();
        var shouldUpdate = false;
        var jwt = _requestUtilities.GetRequestJwt(authorization);
        var userId = jwt.Claims.First(c => c.Type == "UserId").Value;
        var user = session.Query<User>().First(u => u.Id == userId);

        if (body.OldPassword != null && body.NewPassword != null)
        {
            if (!PasswordHasher.Verify(body.OldPassword, user.Password))
            {
                return FailRequest("Old password verification failed.");
            }

            user.Password = PasswordHasher.Hash(body.NewPassword);
            shouldUpdate = true;
        }

        if (body.Email != null)
        {
            user.Email = body.Email;
            shouldUpdate = true;
        }

        if (body.FriendlyName != null)
        {
            user.FriendlyName = body.FriendlyName;
            shouldUpdate = true;
        }

        if (!shouldUpdate)
        {
            return FailRequest("Found no action to perform");
        }

        session.SaveOrUpdate(user);
        return Ok();
    }
}
