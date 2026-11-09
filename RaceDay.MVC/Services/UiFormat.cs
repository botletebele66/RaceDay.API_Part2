using System.Globalization;

namespace RaceDay.MVC.Services;

/// <summary>Display helpers shared by the views: labels, colour classes, times and money.</summary>
public static class UiFormat
{
    /// <summary>1st, 2nd, 3rd, 4th ... 11th, 12th, 13th, 21st, 47th.</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }
        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th",
        };
    }

    /// <summary>Finish time in h:mm:ss (or mm:ss under an hour).</summary>
    public static string Duration(int? totalSeconds)
    {
        if (totalSeconds is null)
        {
            return "—";
        }
        var span = TimeSpan.FromSeconds(totalSeconds.Value);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
    }

    /// <summary>Human label for an enrolment status. Registered enrolments are confirmed; Started means the race is in progress.</summary>
    public static string StatusLabel(string status) => status switch
    {
        "Registered" => "Confirmed",
        "Started" => "In progress",
        "Completed" => "Completed",
        "Withdrawn" => "Withdrawn",
        _ => status,
    };

    /// <summary>Badge colour for an enrolment status: green confirmed, amber in progress, blue completed, red withdrawn.</summary>
    public static string StatusClass(string status) => status switch
    {
        "Registered" => "status-confirmed",
        "Started" => "status-pending",
        "Completed" => "status-completed",
        "Withdrawn" => "status-withdrawn",
        _ => "status-other",
    };

    public static string EventTypeClass(string eventType) => eventType switch
    {
        EventTypes.Running => "type-running",
        EventTypes.Walking => "type-walking",
        EventTypes.Cycling => "type-cycling",
        _ => "type-other",
    };

    public static string EventTypeIcon(string eventType) => eventType switch
    {
        EventTypes.Running => "fa-person-running",
        EventTypes.Walking => "fa-person-walking",
        EventTypes.Cycling => "fa-person-biking",
        _ => "fa-flag-checkered",
    };

    /// <summary>South African rand, for example "R 350,00"; zero is shown as "Free".</summary>
    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : "R " + amount.ToString("N2", SouthAfricaTime.Culture);

    /// <summary>"Free", "R 100,00" for a single fee, or "From R 100,00" when the fees differ.</summary>
    public static string FeeFrom(IReadOnlyCollection<decimal> fees)
    {
        if (fees.Count == 0)
        {
            return "Categories to be announced";
        }
        var cheapest = fees.Min();
        return cheapest == 0 ? "Free entry options" : "From " + Money(cheapest);
    }

    public static string LongDate(DateTime localTime) => localTime.ToString("ddd d MMM yyyy", SouthAfricaTime.Culture);

    public static string ShortTime(DateTime localTime) => localTime.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Distance in kilometres without trailing zeros: 42, 21.1, 109.</summary>
    public static string Km(decimal distance) => distance.ToString("0.##", CultureInfo.InvariantCulture) + " km";
}
