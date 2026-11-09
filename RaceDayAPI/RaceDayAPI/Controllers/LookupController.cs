using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

// Read-only helpers the MVC front end needs to build its forms and dashboard.
[ApiController]
public class LookupController : ControllerBase
{
    private readonly AppDbContext _db;

    public LookupController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/eventtypes ──────────────────────────────────
    /// <summary>List the event types (Run, Walk, Cycle) for the create/edit form.</summary>
    [HttpGet("api/eventtypes")]
    public async Task<IActionResult> GetEventTypes()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        var types = await _db.EventTypes.OrderBy(t => t.EventTypeId).ToListAsync();
        return Ok(types.Select(t => new EventTypeResponse
        {
            EventTypeId = t.EventTypeId,
            TypeName    = t.TypeName
        }));
    }

    // ── GET /api/organiser/events ────────────────────────────
    /// <summary>
    /// The logged-in Organiser's own events with enrolment counts (dashboard).
    /// </summary>
    [HttpGet("api/organiser/events")]
    public async Task<IActionResult> GetMyEvents()
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        int organiserId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

        var events = await _db.Events
            .Include(e => e.EventType)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new OrganiserEventSummary
            {
                EventId        = e.EventId,
                Name           = e.Name,
                EventDate      = e.EventDate,
                Location       = e.Location,
                DistanceKm     = e.DistanceKm,
                EventType      = e.EventType.TypeName,
                BannerImageUrl = e.BannerImageUrl,
                EnrolmentCount = e.Enrolments.Count,
                CategoryCount  = e.Categories.Count
            })
            .ToListAsync();

        return Ok(events);
    }
}
