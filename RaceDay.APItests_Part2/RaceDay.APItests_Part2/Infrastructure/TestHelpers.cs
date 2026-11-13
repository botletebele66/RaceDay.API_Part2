using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;
using Xunit;

namespace RaceDay.APItests_Part2.Infrastructure;

/// <summary>A logged-in test user: its own HTTP client (with session cookie) plus identifiers.</summary>
public sealed record TestAccount(HttpClient Client, int UserId, string Email, string Role);

public static class TestHelpers
{
    public const string Password = "Secret123";

    public static string UniqueEmail(string prefix) => $"{prefix.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";

    /// <summary>Registers an account through the public API and logs it in, returning a client that holds the session cookie.</summary>
    public static async Task<TestAccount> CreateAccountAsync(
        this RaceDayWebApplicationFactory factory,
        string role,
        DateTime? dateOfBirth = null,
        string? email = null)
    {
        var client = factory.CreateClient();
        email ??= UniqueEmail(role);

        var body = new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = Password,
            role,
            organiserInviteCode = role == Roles.Organiser ? RaceDayWebApplicationFactory.OrganiserInviteCode : null,
            companyName = role == Roles.Organiser ? "Test Organiser Co" : null,
            registrationNumber = role == Roles.Organiser ? $"REG-{Guid.NewGuid():N}"[..16] : null,
            dateOfBirth,
        };

        var registration = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var json = await login.ReadJsonAsync();
        return new TestAccount(client, json.IdOf("userId"), email, role);
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static int IdOf(this JsonElement element, string property) => element.GetProperty(property).GetInt32();

    /// <summary>Creates a future running event through the API with one category (10km, R100, optional age restriction).</summary>
    public static async Task<int> CreateEventAsync(this HttpClient organiser, int? maxParticipants = null, int? ageRestriction = null)
    {
        var body = new
        {
            eventName = $"Test Run {Guid.NewGuid():N}"[..20],
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Durban",
            eventType = "Running",
            description = "Integration test event",
            maxParticipants,
            categories = new[]
            {
                new { categoryName = "10km", distance = 10m, entryFee = 100m, ageRestriction },
            },
        };

        var response = await organiser.PostAsJsonAsync("/api/events", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).IdOf("eventId");
    }

    /// <summary>Returns the first category ID of an event, read from the public event endpoint.</summary>
    public static async Task<int> FirstCategoryIdAsync(this HttpClient client, int eventId)
    {
        var response = await client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        return json.GetProperty("categories")[0].IdOf("categoryId");
    }

    public static async Task<T> WithDbAsync<T>(this RaceDayWebApplicationFactory factory, Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public static Task<int> ParticipantIdOfAsync(this RaceDayWebApplicationFactory factory, int userId) =>
        factory.WithDbAsync(db => db.Participants.Where(p => p.UserID == userId).Select(p => p.ParticipantID).SingleAsync());

    public static Task<int> OrganiserIdOfAsync(this RaceDayWebApplicationFactory factory, int userId) =>
        factory.WithDbAsync(db => db.Organisers.Where(o => o.UserID == userId).Select(o => o.OrganiserID).SingleAsync());

    /// <summary>Inserts an event that took place a week ago, with one 10km category owned by the given organiser user.</summary>
    public static Task<(int EventId, int CategoryId)> SeedPastEventAsync(
        this RaceDayWebApplicationFactory factory,
        int organiserUserId,
        int? ageRestriction = null) =>
        factory.WithDbAsync(async db =>
        {
            var organiser = await db.Organisers.SingleAsync(o => o.UserID == organiserUserId);
            var ev = new Event
            {
                OrganiserID = organiser.OrganiserID,
                EventName = $"Past Race {Guid.NewGuid():N}"[..20],
                EventDate = DateTime.UtcNow.AddDays(-7),
                Location = "Durban",
                EventType = EventTypes.Running,
            };
            var category = new EventCategory { CategoryName = "10km", Distance = 10, EntryFee = 100, AgeRestriction = ageRestriction };
            ev.Categories.Add(category);
            db.Events.Add(ev);
            await db.SaveChangesAsync();
            return (ev.EventID, category.CategoryID);
        });

    /// <summary>Inserts an enrolment directly, used when a test needs a past event (which the API will not let a participant enter).</summary>
    public static Task<int> SeedEnrolmentAsync(this RaceDayWebApplicationFactory factory, int participantId, int categoryId) =>
        factory.WithDbAsync(async db =>
        {
            var enrolment = new Enrolment { ParticipantID = participantId, CategoryID = categoryId, Status = EnrolmentStatuses.Registered };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();
            return enrolment.EnrolmentID;
        });
}
