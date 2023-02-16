namespace Identity.Middlewares;

public class ValidateTokenMiddleware
{
    private readonly RequestDelegate _next;

    public ValidateTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<ValidateTokenMiddleware> logger)
    {
        logger.Log(LogLevel.Information, "In custom middleware");
        logger.Log(LogLevel.Information, "Path: {path}", context.Request.Path);
        await _next(context);
    }
}

public static class ValidateTokenMiddlewareExtensions
{
    public static IApplicationBuilder ValidateTokenMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidateTokenMiddleware>();
    }
}
