namespace RaceDayAPI.Models;

// Lookup table: Run, Walk, Cycle
public class EventType
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
