using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Domain;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.Registration;
using MdsCloud.Identity.Settings;
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
    private readonly ISettings _settings;
    private readonly IRequestUtilities _requestUtilities;

    public RegistrationController(
        ILogger<RegistrationController> logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _settings = settings;
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
    public IActionResult Register([FromBody] RegistrationRequestBody body)
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

        var bypassActivation = bool.Parse(_settings["MdsSettings:BypassUserActivation"] ?? "False");
        var newAccount = new Account
        {
            Name = body.AccountName,
            Created = DateTime.UtcNow,
            IsActive = bypassActivation,
        };
        var newUser = new User
        {
            Id = body.UserId,
            Account = newAccount,
            Created = DateTime.UtcNow,
            FriendlyName = body.FriendlyName,
            ActivationCode = bypassActivation ? null : RandomStringGenerator.GenerateString(32),
            IsPrimary = true,
            Email = body.Email,
            Password = PasswordHasher.Hash(body.Password),
            IsActive = bypassActivation,
        };

        newAccount.Users.Add(newUser);

        session.SaveOrUpdate(newAccount);
        session.SaveOrUpdate(newUser);
        transaction.Commit();

        _logger.LogWithMetadata(
            LogLevel.Debug,
            "Successfully registered new user",
            this.Request.GetMdsTraceId(),
            new { AccountId = newAccount.Id, UserId = newUser.Id, }
        );
        return Created(
            string.Empty,
            new { AccountId = newAccount.Id.ToString(), Status = "Success", }
        );
    }
}
