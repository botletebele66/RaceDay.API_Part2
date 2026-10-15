using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.API_Part2.Helper;

namespace RaceDay.API_Part2.Attributes;

/// <summary>
/// Requires a logged-in server-side session. When roles are supplied, the session role must be one of them.
/// Returns 401 when there is no valid session and 403 when the session's role is not permitted.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRoleAttribute(params string[] roles) => _roles = roles;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync(context.HttpContext.RequestAborted);

        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.UserRole);

        if (userId is null || string.IsNullOrEmpty(role))
        {
            context.Result = ErrorResult(StatusCodes.Status401Unauthorized, "Unauthorized", "Log in to access this resource.");
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = ErrorResult(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"This action requires the {string.Join(" or ", _roles)} role.");
        }
    }

    private static ObjectResult ErrorResult(int status, string title, string detail) =>
        new(new ProblemDetails { Status = status, Title = title, Detail = detail }) { StatusCode = status };
}
