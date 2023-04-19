namespace MdsCloud.Identity.Business.DTOs;

public class ArgsWithTrace<T>
    where T : class
{
    public string? MdsTraceId { get; init; }
    public T Data { get; init; }
}
