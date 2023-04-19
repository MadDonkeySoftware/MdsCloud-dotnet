namespace MdsCloud.Identity.Business.DTOs;

public class ArgWithTrace<T>
{
    public string? MdsTraceId { get; init; }
    public T Arg { get; init; }
}
