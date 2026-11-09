using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Shared helpers for every controller that talks to the API.
public abstract class AppController : Controller
{
    protected readonly IRaceDayApiClient Api;

    protected AppController(IRaceDayApiClient api)
    {
        Api = api;
    }

    // One-off banner message shown at the top of the next page
    protected void Flash(string message, string kind = "success")
    {
        TempData["FlashMessage"] = message;
        TempData["FlashKind"]    = kind;
    }

    // Send each role to its own landing page
    protected IActionResult RedirectToRoleHome() =>
        HttpContext.Session.GetString(SessionKeys.UserRole) == SessionKeys.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Events");

    // If the API says our session there has ended, log the user out of this app too.
    // Returns a redirect when that happened, otherwise null.
    protected IActionResult? HandleExpiredSession<T>(ApiResult<T> result)
    {
        if (!result.SessionExpired) return null;

        HttpContext.Session.Clear();
        Flash("Your session has ended. Please log in again.", "warning");
        return RedirectToAction("Login", "Account");
    }

    // Does this event belong to the logged-in Organiser?
    protected async Task<bool> OwnsEventAsync(int eventId)
    {
        var mine = await Api.GetMyOrganiserEventsAsync();
        return mine.Success && mine.Data != null && mine.Data.Any(e => e.EventId == eventId);
    }
}
