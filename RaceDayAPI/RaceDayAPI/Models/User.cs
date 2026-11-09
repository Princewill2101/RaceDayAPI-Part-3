namespace RaceDayAPI.Models;

// Represents both Organisers and Participants.
// The Role column tells us which type of user this is.
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;       // Never stored as plain text
    public string Role { get; set; } = string.Empty;               // "Organiser" or "Participant"
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (EF Core uses these to build the JOINs)
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
