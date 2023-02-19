namespace Identity.Middlewares;

public class CrossSystemTraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public CrossSystemTraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CrossSystemTraceIdMiddleware> logger)
    {
        // logger.Log(LogLevel.Information, "In custom middleware");
        // logger.Log(LogLevel.Information, "Path: {path}", context.Request.Path);

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
