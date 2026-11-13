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

    /// <summary>A stock photo for a race location, matched on town or region name. Unknown places get a generic road photo.</summary>
    public static string LocationPhotoUrl(string? location)
    {
        var name = location ?? string.Empty;
        string photo;
        if (ContainsAny(name, "cape town", "table mountain", "stellenbosch", "paarl", "clifton", "camps bay"))
        {
            photo = "photo-1506905925346-21bda4d32df4";
        }
        else if (ContainsAny(name, "pietermaritzburg", "maritzburg"))
        {
            photo = "photo-1470071459604-3b5ec3a7fe05";
        }
        else if (ContainsAny(name, "durban", "kwazulu", "umhlanga", "ballito"))
        {
            photo = "photo-1507525428034-b723cf961d3e";
        }
        else if (ContainsAny(name, "soweto"))
        {
            photo = "photo-1480714378408-67cf0d13bc1b";
        }
        else if (ContainsAny(name, "johannesburg", "joburg", "sandton", "gauteng", "midrand"))
        {
            photo = "photo-1449824913935-59a10b8d2000";
        }
        else if (ContainsAny(name, "pretoria", "tshwane", "centurion"))
        {
            photo = "photo-1476514525535-07fb3b4ae5f1";
        }
        else if (ContainsAny(name, "port elizabeth", "gqeberha"))
        {
            photo = "photo-1519046904884-53103b34b206";
        }
        else if (ContainsAny(name, "bloemfontein", "free state"))
        {
            photo = "photo-1509316785289-025f5b846b35";
        }
        else if (ContainsAny(name, "knysna", "garden route", "george", "mossel bay"))
        {
            photo = "photo-1441974231531-c6227db76b6e";
        }
        else
        {
            photo = "photo-1461896836934-ffe607ba8211";
        }
        return "https://images.unsplash.com/" + photo + "?w=800&h=450&q=80&auto=format&fit=crop";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Distance in kilometres without trailing zeros: 42, 21.1, 109.</summary>
    public static string Km(decimal distance) => distance.ToString("0.##", CultureInfo.InvariantCulture) + " km";
}
