namespace MdsCloud.DbTooling.Extensions;

public static class DateTimeExtensions
{
    public static DateTime FromUnixTimestamp(this double timestamp)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return origin.AddSeconds(timestamp);
    }

    public static double ToUnixTimestamp(this DateTime dateTime)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var diff = dateTime.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}
