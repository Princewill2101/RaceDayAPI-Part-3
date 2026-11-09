using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

// ── Events ───────────────────────────────────────────────────
public class CreateEventRequest
{
    /// <example>Comrades Marathon</example>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <example>Iconic ultramarathon between Pietermaritzburg and Durban.</example>
    public string? Description { get; set; }

    /// <example>2026-06-14T07:00:00</example>
    [Required]
    public DateTime EventDate { get; set; }

    /// <example>Pietermaritzburg, KZN</example>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <example>87.00</example>
    [Required]
    public decimal DistanceKm { get; set; }

    /// <example>1</example>
    [Required]
    public int EventTypeId { get; set; }
}

public class UpdateEventRequest
{
    /// <example>Comrades Marathon</example>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <example>Iconic ultramarathon between Pietermaritzburg and Durban.</example>
    public string? Description { get; set; }

    /// <example>2026-06-14T07:00:00</example>
    [Required]
    public DateTime EventDate { get; set; }

    /// <example>Pietermaritzburg, KZN</example>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <example>87.00</example>
    [Required]
    public decimal DistanceKm { get; set; }

    /// <example>1</example>
    [Required]
    public int EventTypeId { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();
}

// ── Categories ───────────────────────────────────────────────
public class CreateCategoryRequest
{
    /// <example>Senior</example>
    [Required]
    public string CategoryName { get; set; } = string.Empty;

    /// <example>20</example>
    public int? MinAge { get; set; }

    /// <example>59</example>
    public int? MaxAge { get; set; }

    /// <example>21.1</example>
    public decimal? DistanceKm { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

// ── Enrolments ───────────────────────────────────────────────
public class CreateEnrolmentRequest
{
    /// <example>1</example>
    [Required]
    public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public bool HasResult { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
}

// ── Results ──────────────────────────────────────────────────
public class CreateResultRequest
{
    /// <example>01:23:45</example>
    public TimeSpan? FinishTime { get; set; }

    /// <example>47</example>
    public int? FinishPosition { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }

    // Number of finishers with a recorded position in the same event (e.g. "47th of 312")
    public int TotalFinishers { get; set; }
}

// ── Part 3 additions ─────────────────────────────────────────
public class EventTypeResponse
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}

// Organiser dashboard row: an event plus how many people have enrolled
public class OrganiserEventSummary
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public int CategoryCount { get; set; }
}
