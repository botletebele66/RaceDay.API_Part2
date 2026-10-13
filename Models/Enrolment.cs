namespace RaceDay.API_Part2.Models
{

    public class Enrolment
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Registered"; // "Registered", "Completed", etc.
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Participant Participant { get; set; } = null!;
        public EventCategory Category { get; set; } = null!;
        public Result? Result { get; set; }
    }
}