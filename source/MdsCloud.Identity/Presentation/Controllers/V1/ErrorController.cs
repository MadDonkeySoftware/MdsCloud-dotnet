using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.Presentation.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MdsCloud.Identity.Presentation.Controllers.V1;

[Route("/error")]
[ApiController]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
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
        return new InternalServerErrorResponse(
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
