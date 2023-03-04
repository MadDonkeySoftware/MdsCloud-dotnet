using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.PublicSignature;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MdsCloud.Identity.Controllers.V1;

[Route("v1/publicSignature")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class PublicSignatureController : ControllerBase
{
    private readonly ILogger<PublicSignatureController> _logger;
    private readonly IConfiguration _configuration;

    public PublicSignatureController(
        ILogger<PublicSignatureController> logger,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet(Name = "Get MdsCloud.Identity Public Signature")]
    [ProducesResponseType(typeof(PublicSignatureResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(Description = "", Summary = "", Tags = new[] { "Configuration" })]
    public IActionResult Get()
    {
        var publicKeyText = System.IO.File.ReadAllText(
            _configuration["MdsSettings:Secrets:PublicPath"] ?? ""
        );
        var response = new PublicSignatureResponseBody { Signature = publicKeyText };

        return Ok(response);
    }
}
