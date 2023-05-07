namespace MdsCloud.Identity.Core.Exceptions;

public class InvalidSettingsValueException : Exception
{
    public InvalidSettingsValueException(string? message)
        : base(message) { }
}
