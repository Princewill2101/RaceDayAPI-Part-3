using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class HomeController : Controller
{
    // Landing page for visitors; logged-in users go straight to their own home.
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
        {
            return HttpContext.Session.GetString(SessionKeys.UserRole) == SessionKeys.Organiser
                ? RedirectToAction("Index", "Dashboard")
                : RedirectToAction("Index", "Events");
        }
        return View();
    }

    // Unhandled exceptions end up here - show a friendly page, never the stack trace.
    public IActionResult Error()
    {
        return View("Error", new ErrorViewModel());
    }

    // 404 and other status codes
    public IActionResult StatusPage(int code)
    {
        var vm = code switch
        {
            404 => new ErrorViewModel
            {
                Title   = "Page not found",
                Message = "We couldn't find the page you were looking for."
            },
            _ => new ErrorViewModel()
        };
        Response.StatusCode = code;
        return View("Error", vm);
    }
}
