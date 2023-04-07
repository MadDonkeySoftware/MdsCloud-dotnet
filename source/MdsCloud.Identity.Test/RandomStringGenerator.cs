using System.Security.Cryptography;

namespace MdsCloud.Identity.Test;

public static class RandomStringGenerator
{
    public static string GenerateString(int length)
    {
        var randomNumber = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var value = Convert
            .ToBase64String(randomNumber)
            .Replace("/", string.Empty)
            .Replace("+", string.Empty);

        return value.Substring(0, length);
    }
}
