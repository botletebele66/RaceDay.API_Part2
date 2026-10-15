namespace RaceDay.API_Part2.Helper;

/// <summary>Keys and cookie name for the server-side session. The session stores only the user ID and role.</summary>
public static class SessionKeys
{
    public const string UserId = "UserID";
    public const string UserRole = "UserRole";
    public const string CookieName = ".RaceDay.Session";
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
}

/// <summary>Account roles. Stored in Users.UserRole and in the session.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
    public static readonly string[] All = [Organiser, Participant];
}

/// <summary>Allowed values for Event.EventType (matches the Part 1 CHECK constraint).</summary>
public static class EventTypes
{
    public const string Running = "Running";
    public const string Walking = "Walking";
    public const string Cycling = "Cycling";
    public static readonly string[] All = [Running, Walking, Cycling];
}

/// <summary>Allowed values for Enrolment.Status (matches the Part 1 CHECK constraint).</summary>
public static class EnrolmentStatuses
{
    public const string Registered = "Registered";
    public const string Started = "Started";
    public const string Completed = "Completed";
    public const string Withdrawn = "Withdrawn";
}
