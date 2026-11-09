using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[RoleAuthorize(SessionKeys.Participant)]
public class MyResultsController : AppController
{
    public MyResultsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyResultsAsync();
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
            ViewBag.LoadError = result.Error;

        var list = (result.Data ?? new()).OrderByDescending(r => r.EventDate).ToList();
        return View(list);
    }
}
