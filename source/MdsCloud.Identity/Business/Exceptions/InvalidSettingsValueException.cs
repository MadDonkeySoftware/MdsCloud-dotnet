namespace MdsCloud.Identity.Business.Exceptions;

public class InvalidSettingsValueException : Exception
{
    public InvalidSettingsValueException(string? message)
        : base(message) { }
}
