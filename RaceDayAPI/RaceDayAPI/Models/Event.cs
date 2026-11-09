namespace RaceDayAPI.Models;

public class Event
{
    public int EventId { get; set; }

    // FK → Users (the Organiser who created this event)
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;

    // FK → EventTypes
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
