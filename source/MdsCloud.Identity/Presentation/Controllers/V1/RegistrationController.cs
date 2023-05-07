using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Core.DTOs;
using MdsCloud.Identity.Core.Exceptions;
using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.DTOs;
using MdsCloud.Identity.Presentation.DTOs.Registration;
using MdsCloud.Identity.Presentation.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

// [Route("api/v1/[controller]")]
[Route("v1/register")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class RegistrationController : ControllerBase
{
    private readonly IRequestUtilities _requestUtilities;
    private readonly IAccountService _accountService;

    public RegistrationController(
        IRequestUtilities requestUtilities,
        IAccountService accountService
    )
    {
        _requestUtilities = requestUtilities;
        _accountService = accountService;
    }

    [HttpPost(Name = "Register")]
    [ProducesResponseType(typeof(RegistrationResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Creates a new account and primary user in the system",
        Summary = "New account registration",
        Tags = new[] { "Registration" }
    )]
    public IActionResult Register([FromBody] RegistrationRequestBody body)
    {
        try
        {
            var accountId = _accountService.RegisterNewAccount(
                new ArgsWithTrace<AccountRegistrationArgs>
                {
                    Data = body,
                    MdsTraceId = Request.GetMdsTraceId()
                }
            );

            return Created(
                string.Empty,
                new { AccountId = accountId.ToString(), Status = "Success", }
            );
        }
        catch (UserExistsException)
        {
            _requestUtilities.Delay(10000);
            return BadRequest(
                new BadRequestResponse(
                    new Dictionary<string, string[]>
                    {
                        { "Message", new[] { "Invalid accountName or userName" } }
                    }
                )
            );
        }
        catch (AccountExistsException)
        {
            _requestUtilities.Delay(10000);
            return BadRequest(
                new BadRequestResponse(
                    new Dictionary<string, string[]>
                    {
                        { "Message", new[] { "Invalid accountName or userName" } }
                    }
                )
            );
        }
    }
}
