namespace MdsCloud.Common.API.Logging;

public static class LoggingConstants
{
    private const string headerPrefix = "mds-";
    public const string TraceRequestHeaderKey = $"{headerPrefix}trace-id";
    public const string TraceLogKey = "MdsTraceId";
}
