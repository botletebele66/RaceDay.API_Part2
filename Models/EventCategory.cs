namespace RaceDay.API_Part2.Models
{
    public class EventCategory
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public decimal EntryFee { get; set; }
        public int? AgeRestriction { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Event Event { get; set; } = null!;
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
