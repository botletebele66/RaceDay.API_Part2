using Microsoft.AspNetCore.Http.Features;

namespace RaceDay.MVC.Services;

/// <summary>The signed-in user, read from the MVC session. Safe to call on any request, signed in or not.</summary>
public sealed class CurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession? Session => _accessor.HttpContext?.Features.Get<ISessionFeature>()?.Session;

    public bool IsAuthenticated => UserId.HasValue && !string.IsNullOrEmpty(Session?.GetString(MvcSessionKeys.ApiCookie));

    public int? UserId => int.TryParse(Session?.GetString(MvcSessionKeys.UserId), out var id) ? id : null;

    public string? Role => Session?.GetString(MvcSessionKeys.UserRole);

    public string? Email => Session?.GetString(MvcSessionKeys.Email);

    public string? FirstName => Session?.GetString(MvcSessionKeys.FirstName);

    public string? LastName => Session?.GetString(MvcSessionKeys.LastName);

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public string Initials
    {
        get
        {
            var first = string.IsNullOrEmpty(FirstName) ? "?" : FirstName[..1];
            var last = string.IsNullOrEmpty(LastName) ? string.Empty : LastName[..1];
            return (first + last).ToUpperInvariant();
        }
    }

    public bool IsOrganiser => Role == Roles.Organiser;

    public bool IsParticipant => Role == Roles.Participant;

    public void SignIn(UserDto user)
    {
        var session = Session ?? throw new InvalidOperationException("Session is not available.");
        session.SetString(MvcSessionKeys.UserId, user.UserId.ToString());
        session.SetString(MvcSessionKeys.UserRole, user.Role);
        session.SetString(MvcSessionKeys.FirstName, user.FirstName);
        session.SetString(MvcSessionKeys.LastName, user.LastName);
        session.SetString(MvcSessionKeys.Email, user.Email ?? string.Empty);
    }

    public void UpdateName(string firstName, string lastName, string email)
    {
        if (Session is not { } session)
        {
            return;
        }
        session.SetString(MvcSessionKeys.FirstName, firstName);
        session.SetString(MvcSessionKeys.LastName, lastName);
        session.SetString(MvcSessionKeys.Email, email);
    }

    /// <summary>Ends the MVC session, which also drops the stored API session cookie.</summary>
    public void SignOut() => Session?.Clear();
}
