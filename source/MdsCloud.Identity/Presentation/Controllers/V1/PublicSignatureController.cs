using MdsCloud.Identity.Core.Interfaces;
using MdsCloud.Identity.Presentation.DTOs;
using MdsCloud.Identity.Presentation.DTOs.PublicSignature;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

[Route("v1/publicSignature")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class PublicSignatureController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public PublicSignatureController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    [AllowAnonymous]
    [HttpGet(Name = "Get MdsCloud.Identity Public Signature")]
    [ProducesResponseType(typeof(PublicSignatureResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(Description = "", Summary = "", Tags = new[] { "Configuration" })]
    public IActionResult Get()
    {
        var publicKeyText = _configurationService.GetPublicSignature();
        var response = new PublicSignatureResponseBody { Signature = publicKeyText };

        return Ok(response);
    }
}
