using MdsCloud.Common.API.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MdsCloud.Identity.Controllers.V1;

[DefaultStatusCode(DefaultStatusCode)]
public class FailureObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Initializes a new instance of the <see cref="OkObjectResult"/> class.
    /// </summary>
    /// <param name="value">The content to format into the entity body.</param>
    public FailureObjectResult(object? value)
        : base(value)
    {
        StatusCode = DefaultStatusCode;
    }
}

[Route("/error")]
[ApiController]
public class ErrorController : ControllerBase
{
    private readonly ILogger<PublicSignatureController> _logger;

    public ErrorController(ILogger<PublicSignatureController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult HandleError()
    {
        var errorContext = this.HttpContext.Features.Get<IExceptionHandlerFeature>();
        var mdsTraceId = this.Request.GetMdsTraceId();
        _logger.LogWithMetadata(
            LogLevel.Error,
            "An exception occurred while processing the request",
            new Dictionary<string, dynamic>
            {
                { LoggingConstants.TraceLogKey, mdsTraceId },
                { "ExceptionMessage", errorContext.Error.Message }
            }
        );

        this.Response.SetMdsTraceId(mdsTraceId);
        return new FailureObjectResult(
            new
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "A problem occurred while attempting to process your request",
                Status = 500,
                TraceId = mdsTraceId
            }
        );

        /*
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "An error occurred while processing your request.",
    "status": 500,
    "traceId": "00-3c5c003c387b87a612ebadecd0550397-3c0495906a0992be-00"
         */
    }
}
