using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/events ──────────────────────────────────────
    /// <summary>List all events. Both Organisers and Participants can see this.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        var events = await _db.Events
            .Include(e => e.EventType)
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .ToListAsync();

        return Ok(events.Select(MapToResponse));
    }

    // ── GET /api/events/{id} ─────────────────────────────────
    /// <summary>Get full detail for one event including its categories.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        var ev = await _db.Events
            .Include(e => e.EventType)
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return NotFound(new { message = "Event not found." });

        return Ok(MapToResponse(ev));
    }

    // ── POST /api/events ─────────────────────────────────────
    /// <summary>Create a new event. Organiser only.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        bool typeExists = await _db.EventTypes.AnyAsync(t => t.EventTypeId == req.EventTypeId);
        if (!typeExists)
            return BadRequest(new { message = "Invalid EventTypeId." });

        int organiserId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

        var ev = new Event
        {
            OrganiserId = organiserId,
            EventTypeId = req.EventTypeId,
            Name        = req.Name,
            Description = req.Description,
            EventDate   = req.EventDate,
            Location    = req.Location,
            DistanceKm  = req.DistanceKm
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response
        await _db.Entry(ev).Reference(e => e.EventType).LoadAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return StatusCode(201, MapToResponse(ev));
    }

    // ── PUT /api/events/{id} ─────────────────────────────────
    /// <summary>Update an event. Only the Organiser who created it may do this.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest req)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events
            .Include(e => e.EventType)
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return NotFound(new { message = "Event not found." });

        // Owning Organiser check
        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (ev.OrganiserId != userId)
            return Forbid();

        bool typeExists = await _db.EventTypes.AnyAsync(t => t.EventTypeId == req.EventTypeId);
        if (!typeExists) return BadRequest(new { message = "Invalid EventTypeId." });

        ev.Name        = req.Name;
        ev.Description = req.Description;
        ev.EventDate   = req.EventDate;
        ev.Location    = req.Location;
        ev.DistanceKm  = req.DistanceKm;
        ev.EventTypeId = req.EventTypeId;

        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.EventType).LoadAsync();

        return Ok(MapToResponse(ev));
    }

    // ── DELETE /api/events/{id} ──────────────────────────────
    /// <summary>Delete an event. Only the Organiser who created it may do this.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (ev.OrganiserId != userId)
            return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── GET /api/events/{eventId}/categories ─────────────────
    /// <summary>List all categories for an event. Both roles can view.</summary>
    [HttpGet("{eventId}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .ToListAsync();

        return Ok(categories.Select(c => new CategoryResponse
        {
            CategoryId   = c.CategoryId,
            CategoryName = c.CategoryName,
            MinAge       = c.MinAge,
            MaxAge       = c.MaxAge,
            DistanceKm   = c.DistanceKm
        }));
    }

    // ── POST /api/events/{eventId}/categories ────────────────
    /// <summary>Add a category to an event. Owning Organiser only.</summary>
    [HttpPost("{eventId}/categories")]
    public async Task<IActionResult> AddCategory(int eventId,
        [FromBody] CreateCategoryRequest req)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (ev.OrganiserId != userId) return Forbid();

        var category = new Category
        {
            EventId      = eventId,
            CategoryName = req.CategoryName,
            MinAge       = req.MinAge,
            MaxAge       = req.MaxAge,
            DistanceKm   = req.DistanceKm
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(201, new CategoryResponse
        {
            CategoryId   = category.CategoryId,
            CategoryName = category.CategoryName,
            MinAge       = category.MinAge,
            MaxAge       = category.MaxAge,
            DistanceKm   = category.DistanceKm
        });
    }

    // ── GET /api/events/{eventId}/enrolments ─────────────────
    /// <summary>List all enrolments for an event. Owning Organiser only.</summary>
    [HttpGet("{eventId}/enrolments")]
    public async Task<IActionResult> GetEnrolments(int eventId)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (ev.OrganiserId != userId) return Forbid();

        var enrolments = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category)
            .Include(en => en.Result)
            .Where(en => en.EventId == eventId)
            .ToListAsync();

        return Ok(enrolments.Select(en => new EnrolmentResponse
        {
            EnrolmentId     = en.EnrolmentId,
            EventId         = en.EventId,
            HasResult       = en.Result != null,
            ParticipantName = en.Participant.FullName,
            EventName       = ev.Name,
            CategoryName    = en.Category.CategoryName,
            Status          = en.Status,
            EnrolmentDate   = en.EnrolmentDate
        }));
    }

    // ── POST /api/events/{eventId}/enrolments ────────────────
    /// <summary>Enrol the logged-in Participant into this event under a category.</summary>
    [HttpPost("{eventId}/enrolments")]
    public async Task<IActionResult> Enrol(int eventId,
        [FromBody] CreateEnrolmentRequest req)
    {
        if (!SessionHelper.IsParticipant(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        // Check the category belongs to this event
        var category = await _db.Categories.FindAsync(req.CategoryId);
        if (category == null || category.EventId != eventId)
            return BadRequest(new { message = "Category does not belong to this event." });

        int participantId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

        // Prevent duplicate enrolments
        bool alreadyEnrolled = await _db.Enrolments
            .AnyAsync(en => en.ParticipantId == participantId && en.EventId == eventId);
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId       = eventId,
            CategoryId    = req.CategoryId
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(201, new EnrolmentResponse
        {
            EnrolmentId     = enrolment.EnrolmentId,
            EventId         = eventId,
            ParticipantName = "",          // not needed in this response
            EventName       = ev.Name,
            CategoryName    = category.CategoryName,
            Status          = enrolment.Status,
            EnrolmentDate   = enrolment.EnrolmentDate
        });
    }

    // ── Private mapping helper ───────────────────────────────
    private static EventResponse MapToResponse(Event ev) => new()
    {
        EventId       = ev.EventId,
        Name          = ev.Name,
        Description   = ev.Description,
        EventDate     = ev.EventDate,
        Location      = ev.Location,
        DistanceKm    = ev.DistanceKm,
        EventType     = ev.EventType?.TypeName ?? "",
        OrganizerName = ev.Organiser?.FullName ?? "",
        BannerImageUrl = ev.BannerImageUrl,
        Categories    = ev.Categories.Select(c => new CategoryResponse
        {
            CategoryId   = c.CategoryId,
            CategoryName = c.CategoryName,
            MinAge       = c.MinAge,
            MaxAge       = c.MaxAge,
            DistanceKm   = c.DistanceKm
        }).ToList()
    };
}
