namespace RaceDay.MVC.Services;

/// <summary>
/// Keys for the MVC's own session. The API's session cookie is kept here (server side) and forwarded to the API,
/// so the browser never holds an API credential.
/// </summary>
public static class MvcSessionKeys
{
    public const string CookieName = "RaceDay.MvcSession";
    public const string ApiCookie = "api_cookie";
    public const string UserId = "user_id";
    public const string UserRole = "user_role";
    public const string Email = "email";
    public const string FirstName = "first_name";
    public const string LastName = "last_name";
}

/// <summary>Role names. These match the values the API stores and returns.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Event type and enrolment status values, matching the API.</summary>
public static class EventTypes
{
    public const string Running = "Running";
    public const string Walking = "Walking";
    public const string Cycling = "Cycling";
    public static readonly string[] All = [Running, Walking, Cycling];
}
