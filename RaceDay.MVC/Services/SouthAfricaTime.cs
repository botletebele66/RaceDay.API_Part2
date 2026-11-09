using System.Globalization;

namespace RaceDay.MVC.Services;

/// <summary>
/// South Africa Standard Time (UTC+02:00, no daylight saving). The API stores and returns UTC; forms and pages use local time.
/// A fixed offset is used so the app does not depend on time-zone data being installed in the container.
/// </summary>
public static class SouthAfricaTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(2);

    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-ZA");

    /// <summary>Current local time in South Africa.</summary>
    public static DateTime Now => DateTime.UtcNow + Offset;

    /// <summary>Converts a UTC value from the API into local time for display and form fields.</summary>
    public static DateTime FromApi(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) + Offset;

    /// <summary>Formats a local time with an explicit offset, so the API converts it to the same instant.</summary>
    public static string ToApi(DateTime local) =>
        new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Offset).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    /// <summary>Converts a local time to the UTC value the API stores (used to compare against stored times).</summary>
    public static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Unspecified) - Offset;
}
