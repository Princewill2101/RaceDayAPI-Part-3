using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayWeb.Services;

namespace RaceDayWeb.Filters;

// Put [RoleAuthorize] on a controller to require login, or
// [RoleAuthorize("Organiser")] to require a specific role.
//  - Not logged in      -> redirected to the login page
//  - Logged in, wrong role -> redirected to the "access denied" page
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        int? userId = session.GetInt32(SessionKeys.UserId);

        if (userId == null)
        {
            string returnUrl = context.HttpContext.Request.Path +
                               context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account",
                new { returnUrl });
            return;
        }

        if (_role != null && session.GetString(SessionKeys.UserRole) != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
