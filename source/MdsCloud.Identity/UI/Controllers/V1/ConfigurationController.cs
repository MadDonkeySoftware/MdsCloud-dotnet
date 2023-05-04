using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Business.DTOs;
using MdsCloud.Identity.Business.Interfaces;
using MdsCloud.Identity.Domain.Lookups;
using MdsCloud.Identity.UI.Authorization;
using MdsCloud.Identity.UI.DTOs;
using MdsCloud.Identity.UI.DTOs.Configuration;
using MdsCloud.Identity.UI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.UI.Controllers.V1;

[Route("v1/configuration")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class ConfigurationController : MdsControllerBase<ConfigurationController>
{
    private readonly IConfigurationService _configurationService;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IRequestUtilities requestUtilities,
        IConfigurationService configurationService
    )
        : base(logger, requestUtilities)
    {
        _configurationService = configurationService;
    }

    private string DetermineScope(string? scope)
    {
        // TODO: Determine if request is from local address or external address
        if (string.IsNullOrEmpty(scope))
            return LandscapeUrlScopes.Internal;

        return string.Equals(
            scope,
            LandscapeUrlScopes.External,
            StringComparison.InvariantCultureIgnoreCase
        )
            ? LandscapeUrlScopes.External
            : LandscapeUrlScopes.Internal;
    }

    [AllowAnonymous]
    [HttpGet(Name = "Get MDS Configuration")]
    [ProducesResponseType(typeof(ConfigurationResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Gets the MDS configuration based on where the request originated from",
        Summary = "Get MDS Configuration",
        Tags = new[] { "Configuration" }
    )]
    public IActionResult Get([FromQuery] string? scope)
    {
        var normalizedScope = DetermineScope(scope);
        var response = _configurationService.GetConfiguration(
            new ArgWithTrace<string> { MdsTraceId = Request.GetMdsTraceId(), Arg = normalizedScope }
        );
        return Ok(response);
    }

    [Authorize(Policy = Policies.System)]
    [HttpPost(Name = "Update MDS Configuration")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(
        Description = "Sets the MDS configuration",
        Summary = "Set MDS Configuration",
        Tags = new[] { "Configuration" }
    )]
    public IActionResult Post([FromBody] ConfigurationRequestBody body)
    {
        var saveArgs = new ArgsWithTrace<List<Tuple<string, SaveConfigurationArgs>>>
        {
            MdsTraceId = Request.GetMdsTraceId(),
            Data = new List<Tuple<string, SaveConfigurationArgs>>()
        };

        if (body.Internal != null)
        {
            saveArgs.Data.Add(
                new Tuple<string, SaveConfigurationArgs>(LandscapeUrlScopes.Internal, body.Internal)
            );
        }
        if (body.External != null)
        {
            saveArgs.Data.Add(
                new Tuple<string, SaveConfigurationArgs>(LandscapeUrlScopes.External, body.External)
            );
        }

        _configurationService.SaveConfigurations(saveArgs);

        return Ok();
    }
}
