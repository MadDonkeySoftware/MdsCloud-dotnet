using MdsCloud.Identity.Domain;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.Registration;
using MdsCloud.Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Controllers.V1;

// [Route("api/v1/[controller]")]
[Route("v1/register")] // NOTE: Diverges from pattern due to backwards compatibility
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[AllowAnonymous]
public class RegistrationController : ControllerBase
{
    private readonly ILogger<RegistrationController> _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly IConfiguration _configuration;
    private readonly IRequestUtilities _requestUtilities;

    public RegistrationController(
        ILogger<RegistrationController> logger,
        ISessionFactory sessionFactory,
        IConfiguration configuration,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
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
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        var accountExists = session.Query<Account>().Any(e => e.Name == body.AccountName);
        var userExists = session.Query<User>().Any(e => e.Id == body.UserId);

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
            Created = DateTime.Now.ToUniversalTime(),
            IsActive = bypassActivation,
        };
        var newUser = new User
        {
            Id = body.UserId,
            Account = newAccount,
            Created = DateTime.Now.ToUniversalTime(),
            FriendlyName = body.FriendlyName,
            ActivationCode = RandomStringGenerator.GenerateString(32),
            IsPrimary = true,
            Email = body.Email,
            Password = PasswordHasher.Hash(body.Password),
            IsActive = bypassActivation,
        };

        newAccount.Users.Add(newUser);

        session.SaveOrUpdate(newAccount);
        session.SaveOrUpdate(newUser);
        transaction.Commit();
        // _context.SaveChanges();

        return Ok(new { AccountId = newAccount.Id.ToString(), Status = "Success", });
    }
}
