namespace MdsCloud.Common.API.Logging;

public sealed class MdsLoggerConfiguration
{
    public int EventId { get; set; }

    public string? ServiceName { get; set; }

    public string? LogStashUrl { get; set; }

    public bool Enabled { get; set; }
}
