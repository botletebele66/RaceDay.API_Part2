using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Attributes;

/// <summary>
/// Protects a controller or action. Signed-out visitors are sent to the sign-in page and then returned here.
/// Signed-in users with the wrong role see an access-denied page. With no role, any signed-in user is allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RequireRoleAttribute(string? role = null) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var signedIn = !string.IsNullOrEmpty(session.GetString(MvcSessionKeys.UserId))
            && !string.IsNullOrEmpty(session.GetString(MvcSessionKeys.ApiCookie));

        if (!signedIn)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
            return;
        }

        if (_role is not null && session.GetString(MvcSessionKeys.UserRole) != _role)
        {
            context.Result = new ViewResult { ViewName = "~/Views/Shared/AccessDenied.cshtml", StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
