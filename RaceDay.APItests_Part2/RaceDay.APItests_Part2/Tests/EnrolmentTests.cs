using System.Net;
using System.Net.Http.Json;
using RaceDay.API_Part2.Helper;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

public class EnrolmentTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    private static async Task<int> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).IdOf("enrolmentId");
    }

    [Fact]
    public async Task Participant_EnrolsInCategory_Returns201_AndOrganiserSeesTheEnrolment()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var enrolmentId = await EnrolAsync(participant.Client, categoryId);

        var list = await (await organiser.Client.GetAsync($"/api/events/{eventId}/enrolments")).ReadJsonAsync();
        Assert.Contains(list.EnumerateArray(), e =>
            e.GetProperty("enrolmentId").GetInt32() == enrolmentId &&
            e.GetProperty("email").GetString() == participant.Email);
    }

    [Fact]
    public async Task Participant_EnrolsTwiceInSameCategory_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        await EnrolAsync(participant.Client, categoryId);

        var second = await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var response = await organiser.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AgeRestrictedCategory_WhenUnderage_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var youngster = await _factory.CreateAccountAsync(Roles.Participant, DateTime.UtcNow.AddYears(-17).AddMonths(-6));
        var eventId = await organiser.Client.CreateEventAsync(ageRestriction: 18);
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var response = await youngster.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AgeRestrictedCategory_WhenOldEnough_Returns201()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var adult = await _factory.CreateAccountAsync(Roles.Participant, DateTime.UtcNow.AddYears(-30));
        var eventId = await organiser.Client.CreateEventAsync(ageRestriction: 18);
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var response = await adult.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AgeRestrictedCategory_WithoutDateOfBirth_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync(ageRestriction: 18);
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenEventIsFull_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var first = await _factory.CreateAccountAsync(Roles.Participant);
        var second = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync(maxParticipants: 1);
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        await EnrolAsync(first.Client, categoryId);

        var response = await second.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_ThenEnrolAgain_ReactivatesTheSameEnrolment()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var firstId = await EnrolAsync(participant.Client, categoryId);

        var withdraw = await participant.Client.DeleteAsync($"/api/enrolments/{firstId}");
        var secondId = await EnrolAsync(participant.Client, categoryId);

        Assert.Equal(HttpStatusCode.NoContent, withdraw.StatusCode);
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task Withdraw_TwiceForSameEnrolment_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var enrolmentId = await EnrolAsync(participant.Client, categoryId);
        await participant.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        var second = await participant.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotWithdrawAnotherParticipantsEnrolment_Returns403()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var owner = await _factory.CreateAccountAsync(Roles.Participant);
        var intruder = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var enrolmentId = await EnrolAsync(owner.Client, categoryId);

        var response = await intruder.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ListsOwnEnrolments()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var enrolmentId = await EnrolAsync(participant.Client, categoryId);

        var response = await participant.Client.GetAsync("/api/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.ReadJsonAsync();
        Assert.Contains(list.EnumerateArray(), e => e.GetProperty("enrolmentId").GetInt32() == enrolmentId);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolmentList_Returns403()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();

        var response = await participant.Client.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEventEnrolmentList_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Organiser);
        var other = await _factory.CreateAccountAsync(Roles.Organiser);
        var eventId = await owner.Client.CreateEventAsync();

        var response = await other.Client.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InEventThatHasAlreadyTakenPlace_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var (_, categoryId) = await _factory.SeedPastEventAsync(organiser.UserId);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithActiveEnrolment_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        await EnrolAsync(participant.Client, categoryId);

        var response = await organiser.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WhenOnlyWithdrawnEnrolmentsRemain_Returns204()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var enrolmentId = await EnrolAsync(participant.Client, categoryId);
        await participant.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        var response = await organiser.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
