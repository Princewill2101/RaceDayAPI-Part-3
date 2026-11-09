using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[RoleAuthorize(SessionKeys.Organiser)]
public class DashboardController : AppController
{
    public DashboardController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyOrganiserEventsAsync();
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
            ViewBag.LoadError = result.Error;

        return View(new DashboardViewModel { Events = result.Data ?? new() });
    }
}
