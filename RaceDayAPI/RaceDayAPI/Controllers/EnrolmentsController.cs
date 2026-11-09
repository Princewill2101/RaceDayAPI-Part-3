using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/enrolments/me ───────────────────────────────
    /// <summary>
    /// Returns all events the logged-in Participant has enrolled in.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        if (!SessionHelper.IsParticipant(HttpContext.Session))
            return Forbid();

        int participantId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

        var enrolments = await _db.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Where(en => en.ParticipantId == participantId)
            .ToListAsync();

        return Ok(enrolments.Select(en => new EnrolmentResponse
        {
            EnrolmentId     = en.EnrolmentId,
            EventId         = en.EventId,
            ParticipantName = "",
            EventName       = en.Event.Name,
            CategoryName    = en.Category.CategoryName,
            Status          = en.Status,
            EnrolmentDate   = en.EnrolmentDate
        }));
    }

    // ── POST /api/enrolments/{enrolmentId}/result ────────────
    /// <summary>
    /// Record finish time and position for one enrolment.
    /// Owning Organiser only.
    /// </summary>
    [HttpPost("{enrolmentId}/result")]
    public async Task<IActionResult> RecordResult(int enrolmentId,
        [FromBody] CreateResultRequest req)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        // Load the enrolment and verify the Organiser owns the event
        var enrolment = await _db.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Include(en => en.Participant)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (enrolment.Event.OrganiserId != userId)
            return Forbid();

        // One result per enrolment only
        bool resultExists = await _db.Results
            .AnyAsync(r => r.EnrolmentId == enrolmentId);
        if (resultExists)
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Models.Result
        {
            EnrolmentId    = enrolmentId,
            FinishTime     = req.FinishTime,
            FinishPosition = req.FinishPosition
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(201, new ResultResponse
        {
            ResultId        = result.ResultId,
            ParticipantName = enrolment.Participant.FullName,
            EventName       = enrolment.Event.Name,
            EventDate       = enrolment.Event.EventDate,
            CategoryName    = enrolment.Category.CategoryName,
            FinishTime      = result.FinishTime,
            FinishPosition  = result.FinishPosition
        });
    }
}
