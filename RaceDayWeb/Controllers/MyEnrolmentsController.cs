using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

[RoleAuthorize(SessionKeys.Participant)]
public class MyEnrolmentsController : AppController
{
    public MyEnrolmentsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
            ViewBag.LoadError = result.Error;

        var list = (result.Data ?? new()).OrderByDescending(e => e.EnrolmentDate).ToList();
        return View(list);
    }
}
