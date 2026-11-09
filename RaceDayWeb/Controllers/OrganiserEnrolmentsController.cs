using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Organiser: see who has entered a specific event.
[RoleAuthorize(SessionKeys.Organiser)]
public class OrganiserEnrolmentsController : AppController
{
    public OrganiserEnrolmentsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        if (!await OwnsEventAsync(eventId)) return RedirectToAction("AccessDenied", "Account");

        var ev = await Api.GetEventAsync(eventId);
        var expired = HandleExpiredSession(ev);
        if (expired != null) return expired;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);

        if (!ev.Success || ev.Data == null)
        {
            Flash(ev.Error, "danger");
            return RedirectToAction("Index", "ManageEvents");
        }
        if (!enrolments.Success)
            ViewBag.LoadError = enrolments.Error;

        return View(new EnrolmentsPageViewModel
        {
            Event = ev.Data,
            Enrolments = (enrolments.Data ?? new()).OrderBy(e => e.EnrolmentDate).ToList()
        });
    }
}
