namespace RaceDayAPI.Models;

// One result per enrolment — recorded by the Organiser after the event.
public class Result
{
    public int ResultId { get; set; }

    // FK → Enrolments (UNIQUE — one result per enrolment)
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
