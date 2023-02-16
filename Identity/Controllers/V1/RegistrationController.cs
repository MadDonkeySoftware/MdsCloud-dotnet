using Identity.Domain;
using Identity.DTOs;
using Identity.DTOs.Registration;
using Identity.Repo;
using Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.Controllers.V1;

// [Route("api/v1/[controller]")]
[Route("v1/register")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class RegistrationController : ControllerBase
{
    private readonly ILogger<RegistrationController> _logger;
    private readonly IdentityContext _context;
    private readonly IConfiguration _configuration;
    private readonly IRequestUtilities _requestUtilities;

    public RegistrationController(
        ILogger<RegistrationController> logger,
        IdentityContext context,
        IConfiguration configuration,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _requestUtilities = requestUtilities;
    }

    [HttpPost(Name = "Register")]
    [ProducesResponseType(typeof(RegistrationResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Creates a new account and primary user in the system",
        Summary = "New account registration",
        Tags = new[] { "Registration" }
    )]
    public IActionResult Get([FromBody] RegistrationRequestBody body)
    {
        var accountExists = _context.Accounts.Any(e => e.Name == body.AccountName);
        var userExists = _context.Users.Any(e => e.Id == body.UserId);

        if (accountExists || userExists)
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

        var bypassActivation = bool.Parse(
            _configuration["MdsSettings:BypassUserActivation"] ?? "False"
        );
        var newAccount = new Account
        {
            Name = body.AccountName,
            Created = DateTime.Now,
            IsActive = bypassActivation,
        };
        var newUser = new User
        {
            Id = body.UserId,
            Account = newAccount,
            Created = DateTime.Now,
            FriendlyName = body.FriendlyName,
            ActivationCode = RandomStringGenerator.GenerateString(32),
            IsPrimary = true,
            Email = body.Email,
            Password = PasswordHasher.Hash(body.Password),
            IsActive = bypassActivation,
        };

        newAccount.Users.Add(newUser);
        _context.Accounts.Add(newAccount);
        _context.SaveChanges();

        return Ok(new { AccountId = newAccount.Id.ToString(), Status = "Success", });
    }
}
