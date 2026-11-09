using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Organiser: create, edit and delete events (including the banner image).
[RoleAuthorize(SessionKeys.Organiser)]
public class ManageEventsController : AppController
{
    public ManageEventsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyOrganiserEventsAsync();
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
            ViewBag.LoadError = result.Error;

        return View(result.Data ?? new());
    }

    // ── Create ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new EventFormViewModel
        {
            EventDate = DateTime.Today.AddDays(30).AddHours(7)
        };
        if (!await FillEventTypesAsync(vm)) return RedirectToAction(nameof(Index));
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillEventTypesAsync(model);
            return View("Form", model);
        }

        var result = await Api.CreateEventAsync(ToRequest(model));
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await FillEventTypesAsync(model);
            return View("Form", model);
        }

        // The event exists now, so the banner can be attached to it
        string message = "Your event has been created. Next, add its categories.";
        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(result.Data.EventId, model.Banner);
            if (!upload.Success)
                message = $"Your event was created, but the banner could not be uploaded: {upload.Error}";
        }

        Flash(message);
        return RedirectToAction("Index", "Categories", new { eventId = result.Data.EventId });
    }

    // ── Edit ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!await OwnsEventAsync(id)) return RedirectToAction("AccessDenied", "Account");

        var result = await Api.GetEventAsync(id);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            Flash(result.Error, "danger");
            return RedirectToAction(nameof(Index));
        }

        var ev = result.Data;
        var vm = new EventFormViewModel
        {
            EventId           = ev.EventId,
            Name              = ev.Name,
            Description       = ev.Description,
            EventDate         = ev.EventDate,
            Location          = ev.Location,
            DistanceKm        = ev.DistanceKm,
            ExistingBannerUrl = ev.BannerImageUrl
        };
        if (!await FillEventTypesAsync(vm, ev.EventType)) return RedirectToAction(nameof(Index));
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;

        if (!ModelState.IsValid)
        {
            await FillEventTypesAsync(model);
            return View("Form", model);
        }

        var result = await Api.UpdateEventAsync(id, ToRequest(model));
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await FillEventTypesAsync(model);
            return View("Form", model);
        }

        string message = "Your event has been updated.";
        string kind = "success";
        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                message = $"Event details saved, but the banner could not be uploaded: {upload.Error}";
                kind = "warning";
            }
        }

        Flash(message, kind);
        return RedirectToAction(nameof(Index));
    }

    // ── Delete ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // The database keeps enrolments, so an event with entrants can't be removed
        var mine = await Api.GetMyOrganiserEventsAsync();
        var ev = mine.Data?.FirstOrDefault(e => e.EventId == id);
        if (ev != null && ev.EnrolmentCount > 0)
        {
            Flash($"\"{ev.Name}\" already has {ev.EnrolmentCount} enrolment(s), so it can't be deleted.", "warning");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.DeleteEventAsync(id);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (result.Success) Flash("The event has been deleted.");
        else Flash(result.Error, "danger");

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──────────────────────────────────────────────
    private async Task<bool> FillEventTypesAsync(EventFormViewModel vm, string? selectedTypeName = null)
    {
        var types = await Api.GetEventTypesAsync();
        if (!types.Success || types.Data == null)
        {
            Flash(types.Error, "danger");
            return false;
        }

        vm.EventTypes = types.Data.Select(t => new SelectListItem
        {
            Value = t.EventTypeId.ToString(),
            Text = t.TypeName
        }).ToList();

        // On the edit page, preselect the event's current type
        if (vm.EventTypeId == null && selectedTypeName != null)
            vm.EventTypeId = types.Data.FirstOrDefault(t => t.TypeName == selectedTypeName)?.EventTypeId;

        return true;
    }

    private static EventRequestDto ToRequest(EventFormViewModel m) => new()
    {
        Name        = m.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        EventDate   = m.EventDate!.Value,
        Location    = m.Location.Trim(),
        DistanceKm  = m.DistanceKm!.Value,
        EventTypeId = m.EventTypeId!.Value
    };
}
