using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MdsCloud.ApiCommon.Middleware;

public class CrossSystemTraceIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CrossSystemTraceIdMiddleware> _logger;

    public CrossSystemTraceIdMiddleware(
        RequestDelegate next,
        ILogger<CrossSystemTraceIdMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.Log(LogLevel.Trace, "In custom middleware");
        _logger.Log(LogLevel.Trace, "Path: {path}", context.Request.Path);

        context.Response.Headers.Add(
            "mds-trace-id",
            context.Request.Headers.ContainsKey("mds-trace-id")
                ? context.Request.Headers["mds-trace-id"]
                : Guid.NewGuid().ToString()
        );

        await _next(context);
    }
}

public static class CrossSystemTraceIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCrossSystemTraceId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CrossSystemTraceIdMiddleware>();
    }
}
