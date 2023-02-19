using Identity.Authorization;
using Identity.Domain;
using Identity.Domain.Enums;
using Identity.DTOs;
using Identity.DTOs.Configuration;
using Identity.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.Controllers.V1;

[Route("v1/configuration")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly ISessionFactory _sessionFactory;
    private readonly IRequestUtilities _requestUtilities;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        ISessionFactory sessionFactory,
        IRequestUtilities requestUtilities
    )
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _requestUtilities = requestUtilities;
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
    public IActionResult Get()
    {
        // TODO: Determine if request is from local address or external address
        using var session = _sessionFactory.OpenSession();
        var elements = session
            .Query<LandscapeUrl>()
            .Where(e => e.Scope == LandscapeUrlScopes.Internal);
        var response = new ConfigurationResponseBody();

        foreach (var landscapeUrl in elements)
        {
            switch (landscapeUrl.Key)
            {
                case "allowSelfSignCert":
                    response.AllowSelfSignCert = bool.Parse(landscapeUrl.Value);
                    break;
                case "fsUrl":
                    response.FsUrl = landscapeUrl.Value;
                    break;
                case "identityUrl":
                    response.IdentityUrl = landscapeUrl.Value;
                    break;
                case "nsUrl":
                    response.NsUrl = landscapeUrl.Value;
                    break;
                case "qsUrl":
                    response.QsUrl = landscapeUrl.Value;
                    break;
                case "sfUrl":
                    response.SfUrl = landscapeUrl.Value;
                    break;
                case "smUrl":
                    response.SmUrl = landscapeUrl.Value;
                    break;
            }
        }

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
        // TODO: Determine if request is from local address or external address
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        if (body.Internal != null)
        {
            if (body.Internal.AllowSelfSignCert.HasValue)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.AllowSelfSignCert,
                        body.Internal.AllowSelfSignCert.Value.ToString()
                    )
                );
            }

            if (body.Internal.IdentityUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.IdentityUrl,
                        body.Internal.IdentityUrl
                    )
                );
            }

            if (body.Internal.FsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.FsUrl,
                        body.Internal.FsUrl
                    )
                );
            }

            if (body.Internal.NsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.NsUrl,
                        body.Internal.NsUrl
                    )
                );
            }

            if (body.Internal.QsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.QsUrl,
                        body.Internal.QsUrl
                    )
                );
            }

            if (body.Internal.SfUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.SfUrl,
                        body.Internal.SfUrl
                    )
                );
            }

            if (body.Internal.SmUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.Internal,
                        LandscapeUrlKeys.SmUrl,
                        body.Internal.SmUrl
                    )
                );
            }
        }

        if (body.External != null)
        {
            if (body.External.AllowSelfSignCert.HasValue)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.AllowSelfSignCert,
                        body.External.AllowSelfSignCert.Value.ToString()
                    )
                );
            }

            if (body.External.IdentityUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.IdentityUrl,
                        body.External.IdentityUrl
                    )
                );
            }

            if (body.External.FsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.FsUrl,
                        body.External.FsUrl
                    )
                );
            }

            if (body.External.NsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.NsUrl,
                        body.External.NsUrl
                    )
                );
            }

            if (body.External.QsUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.QsUrl,
                        body.External.QsUrl
                    )
                );
            }

            if (body.External.SfUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.SfUrl,
                        body.External.SfUrl
                    )
                );
            }

            if (body.External.SmUrl != null)
            {
                session.SaveOrUpdate(
                    new LandscapeUrl(
                        LandscapeUrlScopes.External,
                        LandscapeUrlKeys.SmUrl,
                        body.External.SmUrl
                    )
                );
            }
        }

        transaction.Commit();

        return Ok();
    }
}
