namespace RaceDay.API_Part2.Helper;

/// <summary>Normalises incoming dates to UTC. All stored and compared times are UTC.</summary>
public static class TimeHelper
{
    /// <summary>
    /// Values with an offset (for example 2026-12-01T08:00:00+02:00) are converted to UTC.
    /// Values without an offset are treated as UTC, so clients should send an explicit offset or Z.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
