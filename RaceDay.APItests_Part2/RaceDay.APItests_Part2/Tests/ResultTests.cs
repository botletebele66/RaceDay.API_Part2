using System.Net;
using System.Net.Http.Json;
using RaceDay.API_Part2.Helper;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

public class ResultTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ResultTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    /// <summary>Sets up a past event with one enrolment for a fresh participant (the API will not let a participant enter a past event).</summary>
    private async Task<(TestAccount Organiser, TestAccount Participant, int EnrolmentId)> PastEventWithEnrolmentAsync()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var (_, categoryId) = await _factory.SeedPastEventAsync(organiser.UserId);
        var participantId = await _factory.ParticipantIdOfAsync(participant.UserId);
        var enrolmentId = await _factory.SeedEnrolmentAsync(participantId, categoryId);
        return (organiser, participant, enrolmentId);
    }

    [Fact]
    public async Task Organiser_RecordsResult_ForPastEvent_Returns201()
    {
        var (organiser, _, enrolmentId) = await PastEventWithEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId, timeCompletedSeconds = 3600, placementRank = 12,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(3600, json.GetProperty("timeCompleted").GetInt32());
        Assert.Equal(12, json.GetProperty("placementRank").GetInt32());
    }

    [Fact]
    public async Task RecordResult_BeforeEventDate_Returns409()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);
        var participant = await _factory.CreateAccountAsync(Roles.Participant);
        var eventId = await organiser.Client.CreateEventAsync();
        var categoryId = await organiser.Client.FirstCategoryIdAsync(eventId);
        var enrolmentId = (await (await participant.Client.PostAsJsonAsync("/api/enrolments", new { categoryId })).ReadJsonAsync()).IdOf("enrolmentId");

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId, timeCompletedSeconds = 3000, placementRank = 1,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_TwiceForSameEnrolment_Returns409()
    {
        var (organiser, _, enrolmentId) = await PastEventWithEnrolmentAsync();
        var body = new { enrolmentId, timeCompletedSeconds = 3600, placementRank = 5 };
        await organiser.Client.PostAsJsonAsync("/api/results", body);

        var second = await organiser.Client.PostAsJsonAsync("/api/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordResult_ByParticipant_Returns403()
    {
        var (_, participant, enrolmentId) = await PastEventWithEnrolmentAsync();

        var response = await participant.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId, timeCompletedSeconds = 1, placementRank = 1,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_ForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, enrolmentId) = await PastEventWithEnrolmentAsync();
        var otherOrganiser = await _factory.CreateAccountAsync(Roles.Organiser);

        var response = await otherOrganiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId, timeCompletedSeconds = 3600, placementRank = 1,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithZeroTime_Returns400()
    {
        var (organiser, _, enrolmentId) = await PastEventWithEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId, timeCompletedSeconds = 0, placementRank = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewsOwnResult_Returns200()
    {
        var (organiser, participant, enrolmentId) = await PastEventWithEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId, timeCompletedSeconds = 2400, placementRank = 3 });

        var response = await participant.Client.GetAsync($"/api/results/{enrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2400, (await response.ReadJsonAsync()).IdOf("timeCompleted"));
    }

    [Fact]
    public async Task OtherParticipant_CannotViewResult_Returns403()
    {
        var (organiser, _, enrolmentId) = await PastEventWithEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId, timeCompletedSeconds = 2400, placementRank = 3 });
        var stranger = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await stranger.Client.GetAsync($"/api/results/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewsResultForOwnEvent_Returns200()
    {
        var (organiser, _, enrolmentId) = await PastEventWithEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId, timeCompletedSeconds = 2400, placementRank = 3 });

        var response = await organiser.Client.GetAsync($"/api/results/{enrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetResult_BeforeAnyResultIsRecorded_Returns404()
    {
        var (_, participant, enrolmentId) = await PastEventWithEnrolmentAsync();

        var response = await participant.Client.GetAsync($"/api/results/{enrolmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotWithdrawAfterResultIsRecorded_Returns409()
    {
        var (organiser, participant, enrolmentId) = await PastEventWithEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId, timeCompletedSeconds = 2400, placementRank = 3 });

        var response = await participant.Client.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantHistory_OwnerSeesFullHistory_WithResults()
    {
        var (organiser, participant, enrolmentId) = await PastEventWithEnrolmentAsync();
        await organiser.Client.PostAsJsonAsync("/api/results", new { enrolmentId, timeCompletedSeconds = 2700, placementRank = 9 });
        var participantId = await _factory.ParticipantIdOfAsync(participant.UserId);

        var response = await participant.Client.GetAsync($"/api/participants/{participantId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.ReadJsonAsync();
        Assert.Contains(list.EnumerateArray(), h =>
            h.GetProperty("enrolmentId").GetInt32() == enrolmentId &&
            h.GetProperty("timeCompleted").GetInt32() == 2700);
    }

    [Fact]
    public async Task ParticipantHistory_OrganiserSeesOnlyEnrolmentsInOwnEvents()
    {
        var (organiser, participant, _) = await PastEventWithEnrolmentAsync();
        var participantId = await _factory.ParticipantIdOfAsync(participant.UserId);
        var unrelatedOrganiser = await _factory.CreateAccountAsync(Roles.Organiser);

        var ownView = await (await organiser.Client.GetAsync($"/api/participants/{participantId}/history")).ReadJsonAsync();
        var otherView = await (await unrelatedOrganiser.Client.GetAsync($"/api/participants/{participantId}/history")).ReadJsonAsync();

        Assert.True(ownView.GetArrayLength() >= 1);
        Assert.Equal(0, otherView.GetArrayLength());
    }

    [Fact]
    public async Task ParticipantHistory_OtherParticipant_Returns403()
    {
        var (_, participant, _) = await PastEventWithEnrolmentAsync();
        var participantId = await _factory.ParticipantIdOfAsync(participant.UserId);
        var stranger = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await stranger.Client.GetAsync($"/api/participants/{participantId}/history");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
