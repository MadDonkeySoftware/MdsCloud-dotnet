using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.UI.DTOs;
using MdsCloud.Identity.UI.Utils;
using Microsoft.AspNetCore.Mvc;

namespace MdsCloud.Identity.UI.Controllers.V1;

public abstract class MdsControllerBase<T> : ControllerBase
    where T : class
{
    protected readonly ILogger<T> Logger;
    protected readonly IRequestUtilities RequestUtilities;

    protected MdsControllerBase(ILogger<T> logger, IRequestUtilities requestUtilities)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RequestUtilities = requestUtilities;
    }

    /// <summary>
    /// </summary>
    /// <param name="logReason">The internal log message to emit</param>
    /// <param name="userMessage">Optional message to emit to the user</param>
    /// <param name="delay">The amount of time to delay before sending the response</param>
    /// <returns></returns>
    protected virtual BadRequestObjectResult FailRequest(
        string logReason,
        string? userMessage = null,
        int? delay = null
    )
    {
        Logger.LogWithMetadata(
            LogLevel.Debug,
            "Request Failed",
            this.Request.GetMdsTraceId(),
            new { Reason = logReason, UserMessage = userMessage, }
        );

        if (delay != null)
            RequestUtilities.Delay(delay.Value);
        return BadRequest(
            new BadRequestResponse(
                new Dictionary<string, string[]>
                {
                    { "Message", new[] { userMessage ?? "Bad Request" } }
                }
            )
        );
    }
}
