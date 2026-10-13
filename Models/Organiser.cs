namespace RaceDay.API_Part2.Models
{
    public class Organiser
    {
        public int OrganiserID { get; set; }
        public int UserID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}