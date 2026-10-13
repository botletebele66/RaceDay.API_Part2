namespace RaceDay.API_Part2.Models
{
    public class Event
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // "Running", "Walking", "Cycling"
        public string? Description { get; set; }
        public int? MaxParticipants { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Organiser Organiser { get; set; } = null!;
        public ICollection<EventCategory> Categories { get; set; } = new List<EventCategory>();
    }
}