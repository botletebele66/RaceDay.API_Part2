using System.Net;
using System.Net.Http.Json;
using RaceDay.API_Part2.Helper;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

public class EventTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private static object FutureEventBody(string name = "Comrades Test") => new
    {
        eventName = name,
        eventDate = DateTime.UtcNow.AddDays(60),
        location = "Pietermaritzburg to Durban",
        eventType = "Running",
        description = "Test event",
        maxParticipants = 500,
        categories = new[]
        {
            new { categoryName = "Half (21km)", distance = 21m, entryFee = 200m, ageRestriction = 16 },
            new { categoryName = "Full (42km)", distance = 42m, entryFee = 300m, ageRestriction = 18 },
        },
    };

    [Fact]
    public async Task Organiser_CreatesEvent_WithCategories_Returns201()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/events", FutureEventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(2, json.GetProperty("categories").GetArrayLength());
        Assert.Equal("Running", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task EventList_IsPublic_AndIncludesCreatedEvent()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.ReadJsonAsync();
        Assert.Contains(list.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == eventId);
    }

    [Fact]
    public async Task SingleEvent_IsPublic_Returns200_AndUnknownEvent_Returns404()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();
        var anonymous = _factory.CreateClient();

        var found = await anonymous.GetAsync($"/api/events/{eventId}");
        var missing = await anonymous.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task EventList_FilteredByType_ReturnsOnlyThatType()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        await organiser.Client.CreateEventAsync();

        var response = await _factory.CreateClient().GetAsync("/api/events?eventType=Cycling");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.ReadJsonAsync();
        Assert.All(list.EnumerateArray(), e => Assert.Equal("Cycling", e.GetProperty("eventType").GetString()));
    }

    [Fact]
    public async Task EventList_WithUnknownType_Returns400()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events?eventType=Swimming");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await participant.Client.PostAsJsonAsync("/api/events", FutureEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", FutureEventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var body = new { eventName = "Old", eventDate = DateTime.UtcNow.AddDays(-1), location = "X", eventType = "Walking" };

        var response = await organiser.Client.PostAsJsonAsync("/api/events", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownEventType_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var body = new { eventName = "Odd", eventDate = DateTime.UtcNow.AddDays(10), location = "X", eventType = "Swimming" };

        var response = await organiser.Client.PostAsJsonAsync("/api/events", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithDuplicateCategoryNames_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var body = new
        {
            eventName = "Dupes", eventDate = DateTime.UtcNow.AddDays(10), location = "X", eventType = "Running",
            categories = new[]
            {
                new { categoryName = "10km", distance = 10m, entryFee = 50m },
                new { categoryName = "10KM", distance = 10m, entryFee = 50m },
            },
        };

        var response = await organiser.Client.PostAsJsonAsync("/api/events", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesOwnEvent_Returns204()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Renamed Event", eventDate = DateTime.UtcNow.AddDays(45), location = "Cape Town", eventType = "Cycling",
            description = "Updated", maxParticipants = 800,
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await (await organiser.Client.GetAsync($"/api/events/{eventId}")).ReadJsonAsync();
        Assert.Equal("Renamed Event", updated.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task OtherOrganiser_CannotEditEvent_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Organiser);
        var intruder = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Hijacked", eventDate = DateTime.UtcNow.AddDays(45), location = "X", eventType = "Running",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_CapacityBelowActiveEnrolments_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var first = await _factory.CreateAccountAsync(Roles.Participant);
        var second = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync(maxParticipants: 5);
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        await first.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });
        await second.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        // Two active enrolments exist, so capacity 1 must be refused.
        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Shrunk", eventDate = DateTime.UtcNow.AddDays(30), location = "X", eventType = "Running", maxParticipants = 1,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletesEventWithoutEnrolments_Returns204_ThenNotFound()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var delete = await organiser.Client.DeleteAsync($"/api/events/{eventId}");
        var afterDelete = await organiser.Client.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotDeleteEvent_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Organiser);
        var intruder = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ByOwner_Returns201_AndAppearsInPublicList()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var add = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Under 20", distance = 5m, entryFee = 0m, ageRestriction = 0,
        });
        var list = await (await _factory.CreateClient().GetAsync($"/api/events/{eventId}/categories")).ReadJsonAsync();

        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        Assert.Contains(list.EnumerateArray(), c => c.GetProperty("categoryName").GetString() == "Under 20");
    }

    [Fact]
    public async Task AddCategory_ByOtherOrganiser_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Organiser);
        var intruder = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await owner.Client.CreateEventAsync();

        var response = await intruder.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Sneaky", distance = 5m, entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_DuplicateName_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10KM", distance = 10m, entryFee = 100m,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ZeroDistance_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Zero", distance = 0m, entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ByParticipant_Returns403()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await participant.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Mine", distance = 5m, entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ToPastEvent_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var (eventId, _) = await _factory.SeedPastEventAsync(organiser.UserId);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Late", distance = 5m, entryFee = 0m,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithActiveEnrolment_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        var response = await organiser.Client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ByOwner_Returns204()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var response = await organiser.Client.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "10km Fun Run", distance = 10m, entryFee = 120m, ageRestriction = 12,
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
