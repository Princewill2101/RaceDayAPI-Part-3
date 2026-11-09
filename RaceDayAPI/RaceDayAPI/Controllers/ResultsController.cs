using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/results/me ──────────────────────────────────
    /// <summary>
    /// Returns the logged-in Participant's personal race history —
    /// event name, date, category, finish time, and finishing position
    /// across all events they have a recorded result for.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyResults()
    {
        if (!SessionHelper.IsParticipant(HttpContext.Session))
            return Forbid();

        int participantId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

        var results = await _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(en => en.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(en => en.Category)
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .ToListAsync();

        // Count how many finishers each of these events has, so the Participant
        // can see e.g. "47th out of 312".
        var eventIds = results.Select(r => r.Enrolment.EventId).Distinct().ToList();
        var finisherCounts = await _db.Results
            .Where(r => eventIds.Contains(r.Enrolment.EventId) && r.FinishPosition != null)
            .GroupBy(r => r.Enrolment.EventId)
            .Select(g => new { EventId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Total);

        return Ok(results.Select(r => new ResultResponse
        {
            ResultId        = r.ResultId,
            ParticipantName = "",
            EventName       = r.Enrolment.Event.Name,
            EventDate       = r.Enrolment.Event.EventDate,
            CategoryName    = r.Enrolment.Category.CategoryName,
            FinishTime      = r.FinishTime,
            FinishPosition  = r.FinishPosition,
            TotalFinishers  = finisherCounts.TryGetValue(r.Enrolment.EventId, out var t) ? t : 0
        }));
    }
}
