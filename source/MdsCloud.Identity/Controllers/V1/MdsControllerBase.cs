using MdsCloud.Common.API.Logging;
using MdsCloud.Identity.DTOs;
using MdsCloud.Identity.Settings;
using MdsCloud.Identity.Utils;
using Microsoft.AspNetCore.Mvc;
using NHibernate;

namespace MdsCloud.Identity.Controllers.V1;

public abstract class MdsControllerBase : ControllerBase
{
    protected readonly ILogger<ImpersonationController> Logger;
    protected readonly ISessionFactory SessionFactory;
    protected readonly ISettings Settings;
    protected readonly IRequestUtilities RequestUtilities;

    protected MdsControllerBase(
        ILogger<ImpersonationController> logger,
        ISessionFactory sessionFactory,
        ISettings settings,
        IRequestUtilities requestUtilities
    )
    {
        Logger = logger;
        SessionFactory = sessionFactory;
        Settings = settings;
        RequestUtilities = requestUtilities;
    }

    /// <summary>
    /// </summary>
    /// <param name="reason">The internal log message to emit</param>
    /// <returns></returns>
    protected BadRequestObjectResult FailRequest(string logReason, string? userMessage = null)
    {
        Logger.LogWithMetadata(
            LogLevel.Trace,
            "Request Failed",
            this.Request.GetMdsTraceId(),
            new { Reason = logReason, UserMessage = userMessage, }
        );

        RequestUtilities.Delay(10000);
        return BadRequest(
            new BadRequestResponse(
                new Dictionary<string, string[]>
                {
                    {
                        "Message",
                        new[]
                        {
                            userMessage
                                ?? "Could not find account, user, or passwords did not match" // TODO: Update this after tests are in place.
                        }
                    }
                }
            )
        );
    }
}
