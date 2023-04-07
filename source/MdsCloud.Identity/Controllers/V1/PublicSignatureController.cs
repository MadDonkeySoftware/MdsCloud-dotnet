using MadDonkeySoftware.SystemWrappers.IO;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.DTOs.PublicSignature;
using MdsCloud.Identity.Settings;
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
    private readonly ISettings _settings;
    private readonly IFile _file;

    public PublicSignatureController(
        ILogger<PublicSignatureController> logger,
        ISettings settings,
        IFile file
    )
    {
        _logger = logger;
        _settings = settings;
        _file = file;
    }

    [AllowAnonymous]
    [HttpGet(Name = "Get MdsCloud.Identity Public Signature")]
    [ProducesResponseType(typeof(PublicSignatureResponseBody), 200)]
    [ProducesResponseType(typeof(BadRequestResponse), 400)]
    [SwaggerOperation(Description = "", Summary = "", Tags = new[] { "Configuration" })]
    public IActionResult Get()
    {
        var publicKeyText = _file.ReadAllText(_settings["MdsSettings:Secrets:PublicPath"] ?? "");
        var response = new PublicSignatureResponseBody { Signature = publicKeyText };

        return Ok(response);
    }
}
