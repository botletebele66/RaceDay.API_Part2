namespace RaceDay.API_Part2.DTOs
{
    public class CreateEventDto
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxParticipants { get; set; }
    }
}