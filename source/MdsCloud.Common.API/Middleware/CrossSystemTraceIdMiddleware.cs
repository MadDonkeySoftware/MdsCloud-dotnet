using MdsCloud.Common.API.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace MdsCloud.Common.API.Middleware;

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
        string traceId;
        const string key = LoggingConstants.TraceRequestHeaderKey;
        if (context.Request.Headers.ContainsKey(key))
        {
            traceId = context.Request.GetMdsTraceId();
        }
        else
        {
            traceId = Guid.NewGuid().ToString();
            context.Request.Headers.Add(key, traceId);
        }

        context.Response.Headers.Add(key, traceId);
        _logger.LogWithMetadata(
            LogLevel.Trace,
            $"Handling request at path: {context.Request.Path}",
            new Dictionary<string, dynamic> { { LoggingConstants.TraceLogKey, traceId } }
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
