using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Participant: browse upcoming events, view details, enrol.
[RoleAuthorize(SessionKeys.Participant)]
public class EventsController : AppController
{
    public EventsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(string? search, string? type, string? sort)
    {
        var eventsResult = await Api.GetEventsAsync();
        var expired = HandleExpiredSession(eventsResult);
        if (expired != null) return expired;

        var vm = new EventListViewModel { Search = search, Type = type, Sort = sort };

        if (!eventsResult.Success || eventsResult.Data == null)
        {
            ViewBag.LoadError = eventsResult.Error;
            return View(vm);
        }

        // Upcoming events only
        var upcoming = eventsResult.Data.Where(e => e.EventDate >= DateTime.Today).ToList();
        vm.Types = upcoming.Select(e => e.EventType).Distinct().OrderBy(t => t).ToList();

        // Filtering and sorting the list for display
        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            upcoming = upcoming.Where(e =>
                e.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(type))
            upcoming = upcoming.Where(e => e.EventType == type).ToList();

        vm.Events = sort switch
        {
            "distance" => upcoming.OrderBy(e => e.DistanceKm).ToList(),
            "name"     => upcoming.OrderBy(e => e.Name).ToList(),
            _          => upcoming.OrderBy(e => e.EventDate).ToList()
        };

        // Mark events the user has already entered
        var mine = await Api.GetMyEnrolmentsAsync();
        if (mine.Success && mine.Data != null)
            vm.EnrolledEventIds = mine.Data.Select(e => e.EventId).ToHashSet();

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await Api.GetEventAsync(id);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            Flash(result.Error, "danger");
            return RedirectToAction(nameof(Index));
        }

        var vm = new EventDetailsViewModel { Event = result.Data, IsParticipant = true };

        var mine = await Api.GetMyEnrolmentsAsync();
        if (mine.Success && mine.Data != null)
            vm.AlreadyEnrolled = mine.Data.Any(e => e.EventId == id);

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId == null)
        {
            Flash("Please choose a category before entering.", "warning");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await Api.EnrolAsync(eventId, categoryId.Value);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            Flash(result.Error, "danger");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Flash("You're in! Your enrolment has been recorded.");
        return RedirectToAction("Index", "MyEnrolments");
    }
}
