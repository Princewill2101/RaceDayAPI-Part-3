namespace RaceDayAPI.Models;

// Links a Participant to an Event under a specific Category.
// A Participant can only enrol once per event (enforced by unique constraint).
public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Confirmed";   // "Confirmed" or "Cancelled"

    // One enrolment produces at most one result (one-to-one)
    public Result? Result { get; set; }
}
