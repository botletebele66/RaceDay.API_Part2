using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Data;

/// <summary>
/// Creates the schema from the EF model when it does not exist and, optionally, loads demo data.
/// Demo seeding only runs on an empty database and is enabled through the Seed:Demo setting (Development only).
/// </summary>
public static class DbInitializer
{
    /// <summary>Password shared by every demo account. Only seeded when Seed:Demo is true.</summary>
    public const string DemoPassword = "Password123!";

    public static void Initialize(AppDbContext db, bool seedDemoData)
    {
        db.Database.EnsureCreated();

        if (!seedDemoData || db.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hash = PasswordHelper.HashPassword(DemoPassword);

        var org1 = new Organiser { CompanyName = "Comrades Marathon Organisers", PhoneNumber = "011-123-4567", RegistrationNumber = "REG-001" };
        var org2 = new Organiser { CompanyName = "Cape Town Cycle Tours", PhoneNumber = "021-987-6543", RegistrationNumber = "REG-002" };
        var orgUser1 = new User { Email = "org1@raceday.co.za", Password = hash, FirstName = "John", LastName = "Smith", UserRole = Roles.Organiser, Organiser = org1 };
        var orgUser2 = new User { Email = "org2@raceday.co.za", Password = hash, FirstName = "Sarah", LastName = "Johnson", UserRole = Roles.Organiser, Organiser = org2 };

        var part1 = new Participant { DateOfBirth = new DateTime(1990, 5, 15), PhoneNumber = "082-123-4567", EmergencyContact = "Mary Brown - 082-111-1111" };
        var part2 = new Participant { DateOfBirth = new DateTime(1985, 8, 22), PhoneNumber = "083-456-7890", EmergencyContact = "Jane Davis - 083-222-2222" };
        var partUser1 = new User { Email = "participant1@example.com", Password = hash, FirstName = "Alice", LastName = "Brown", UserRole = Roles.Participant, Participant = part1 };
        var partUser2 = new User { Email = "participant2@example.com", Password = hash, FirstName = "Bob", LastName = "Davis", UserRole = Roles.Participant, Participant = part2 };

        var comrades = new Event
        {
            Organiser = org1, EventName = "Comrades Marathon", EventDate = now.AddMonths(9), Location = "Pietermaritzburg to Durban",
            EventType = EventTypes.Running, Description = "The world's longest and oldest ultramarathon.", MaxParticipants = 3000,
        };
        var fullUltra = new EventCategory { Event = comrades, CategoryName = "Full Ultra (87km)", Distance = 87, EntryFee = 350, AgeRestriction = 18 };
        var halfComrades = new EventCategory { Event = comrades, CategoryName = "Half Marathon (21km)", Distance = 21, EntryFee = 200, AgeRestriction = 16 };

        var cycleTour = new Event
        {
            Organiser = org2, EventName = "Cape Town Cycle Tour", EventDate = now.AddMonths(6), Location = "Cape Town",
            EventType = EventTypes.Cycling, Description = "A circular timed cycle race around the Cape Peninsula.", MaxParticipants = 5000,
        };
        var route109 = new EventCategory { Event = cycleTour, CategoryName = "109km Route", Distance = 109, EntryFee = 250, AgeRestriction = 16 };
        var route65 = new EventCategory { Event = cycleTour, CategoryName = "65km Route", Distance = 65, EntryFee = 200, AgeRestriction = 14 };

        var soweto = new Event
        {
            Organiser = org1, EventName = "Soweto Marathon", EventDate = now.AddYears(1), Location = "Soweto",
            EventType = EventTypes.Running, Description = "Urban marathon through the streets of Soweto.", MaxParticipants = 2000,
        };
        var soweto42 = new EventCategory { Event = soweto, CategoryName = "Full Marathon (42km)", Distance = 42, EntryFee = 300, AgeRestriction = 18 };

        // A past event so that results can be recorded for demo participants.
        var parkRun = new Event
        {
            Organiser = org2, EventName = "Cape Town Park Run", EventDate = now.AddMonths(-3), Location = "Cape Town",
            EventType = EventTypes.Running, Description = "Weekly 5km community run.",
        };
        var parkRun5k = new EventCategory { Event = parkRun, CategoryName = "5km Park Run", Distance = 5, EntryFee = 0 };

        var aliceUltra = new Enrolment { Participant = part1, Category = fullUltra, Status = EnrolmentStatuses.Registered };
        var aliceSoweto = new Enrolment { Participant = part1, Category = soweto42, Status = EnrolmentStatuses.Registered };
        var bobHalf = new Enrolment { Participant = part2, Category = halfComrades, Status = EnrolmentStatuses.Registered };
        var aliceParkRun = new Enrolment { Participant = part1, Category = parkRun5k, Status = EnrolmentStatuses.Completed };
        var bobParkRun = new Enrolment { Participant = part2, Category = parkRun5k, Status = EnrolmentStatuses.Completed };

        aliceParkRun.Result = new Result { TimeCompleted = 1500, PlacementRank = 3, DateCompleted = now.AddMonths(-3) };
        bobParkRun.Result = new Result { TimeCompleted = 1620, PlacementRank = 7, DateCompleted = now.AddMonths(-3) };

        db.Users.AddRange(orgUser1, orgUser2, partUser1, partUser2);
        db.Events.AddRange(comrades, cycleTour, soweto, parkRun);
        // Categories are added explicitly: EF only discovers the ones that an enrolment points at.
        db.EventCategories.AddRange(fullUltra, halfComrades, route109, route65, soweto42, parkRun5k);
        db.Enrolments.AddRange(aliceUltra, aliceSoweto, bobHalf, aliceParkRun, bobParkRun);
        db.SaveChanges();
    }
}
