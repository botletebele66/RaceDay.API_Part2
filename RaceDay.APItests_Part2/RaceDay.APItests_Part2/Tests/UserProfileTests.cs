using System.Net;
using System.Net.Http.Json;
using RaceDay.API_Part2.Helper;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

public class UserProfileTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public UserProfileTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_CanViewOwnProfile_IncludingParticipantDetails()
    {
        var dob = new DateTime(1992, 3, 4);
        var account = await _factory.CreateAccountAsync(Roles.Participant, dob);

        var response = await account.Client.GetAsync($"/api/users/{account.UserId}/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(Roles.Participant, json.GetProperty("role").GetString());
        Assert.Equal(dob.Date, json.GetProperty("dateOfBirth").GetDateTime().Date);
    }

    [Fact]
    public async Task Profile_OfAnotherUser_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Participant);
        var other = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await other.Client.GetAsync($"/api/users/{owner.UserId}/profile");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_CanUpdateOwnAccount_AndChangesAreVisible()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var update = await account.Client.PutAsJsonAsync($"/api/users/{account.UserId}", new
        {
            firstName = "Renamed", lastName = "Runner", email = account.Email, phoneNumber = "082 000 0000",
            dateOfBirth = new DateTime(1990, 1, 1), emergencyContact = "Mum - 082 111 2222",
        });
        var profile = await (await account.Client.GetAsync($"/api/users/{account.UserId}/profile")).ReadJsonAsync();

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal("Renamed", profile.GetProperty("firstName").GetString());
        Assert.Equal("Mum - 082 111 2222", profile.GetProperty("emergencyContact").GetString());
    }

    [Fact]
    public async Task UpdateAnotherUsersAccount_Returns403()
    {
        var owner = await _factory.CreateAccountAsync(Roles.Participant);
        var other = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await other.Client.PutAsJsonAsync($"/api/users/{owner.UserId}", new
        {
            firstName = "Hacked", lastName = "Name", email = owner.Email,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmail_ToAnotherAccountsEmail_Returns409()
    {
        var taken = await _factory.CreateAccountAsync(Roles.Participant);
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await account.Client.PutAsJsonAsync($"/api/users/{account.UserId}", new
        {
            firstName = "Test", lastName = "User", email = taken.Email,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateWithoutCompanyName_Returns400()
    {
        var organiser = await _factory.CreateAccountAsync(Roles.Organiser);

        var response = await organiser.Client.PutAsJsonAsync($"/api/users/{organiser.UserId}", new
        {
            firstName = "Org", lastName = "Nizer", email = organiser.Email, registrationNumber = "REG-KEEP-1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_Anonymous_Returns401()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await _factory.CreateClient().GetAsync($"/api/users/{account.UserId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_ByAnotherLoggedInUser_HidesEmailAddress()
    {
        var target = await _factory.CreateAccountAsync(Roles.Organiser);
        var viewer = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await viewer.Client.GetAsync($"/api/users/{target.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, json.GetProperty("email").ValueKind);
    }
}
