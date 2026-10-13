namespace RaceDay.API_Part2.Models
{

    public class Result
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public int? TimeCompleted { get; set; } // in seconds
        public int? PlacementRank { get; set; }
        public DateTime DateCompleted { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Enrolment Enrolment { get; set; } = null!;
    }
}