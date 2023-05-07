namespace MdsCloud.Identity.Core.DTOs;

public class ArgsWithTrace<T>
{
    public string? MdsTraceId { get; init; }
    public T? Data { get; init; }
}
