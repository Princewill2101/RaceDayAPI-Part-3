using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Organiser: capture finish times and positions after an event.
[RoleAuthorize(SessionKeys.Organiser)]
public class ResultsEntryController : AppController
{
    public ResultsEntryController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        if (!await OwnsEventAsync(eventId)) return RedirectToAction("AccessDenied", "Account");

        var ev = await Api.GetEventAsync(eventId);
        var expired = HandleExpiredSession(ev);
        if (expired != null) return expired;

        if (!ev.Success || ev.Data == null)
        {
            Flash(ev.Error, "danger");
            return RedirectToAction("Index", "ManageEvents");
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
            ViewBag.LoadError = enrolments.Error;

        return View(new ResultsEntryPageViewModel
        {
            Event = ev.Data,
            Enrolments = (enrolments.Data ?? new()).OrderBy(e => e.ParticipantName).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(ResultFormViewModel form)
    {
        // Results are entered after the event, so one of the two values is needed
        TimeSpan? finishTime = null;
        if (!string.IsNullOrWhiteSpace(form.FinishTime))
        {
            if (!TimeSpan.TryParse(form.FinishTime, out var parsed))
                ModelState.AddModelError(nameof(form.FinishTime), "Enter the finish time as hh:mm:ss, e.g. 01:23:45.");
            else
                finishTime = parsed;
        }
        if (string.IsNullOrWhiteSpace(form.FinishTime) && form.FinishPosition == null)
            ModelState.AddModelError(string.Empty, "Enter a finish time, a finish position, or both.");

        if (!ModelState.IsValid)
        {
            string errors = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            Flash(errors, "danger");
            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }

        var result = await Api.RecordResultAsync(form.EnrolmentId, new ResultRequestDto
        {
            FinishTime     = finishTime,
            FinishPosition = form.FinishPosition
        });
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (result.Success) Flash($"Result recorded for {result.Data?.ParticipantName}.");
        else Flash(result.Error, "danger");

        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }
}
