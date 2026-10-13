using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API_Part2.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserID");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (_roles.Length > 0 && !_roles.Contains(userRole))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}