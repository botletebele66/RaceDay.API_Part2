using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

public class AuthenticationTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthenticationTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Participant_Returns201_AndStoresOnlyAHash()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("hash");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Alice", lastName = "Runner", email, password = TestHelpers.Password, role = Roles.Participant,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var stored = await _factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == email));
        Assert.NotEqual(TestHelpers.Password, stored.Password);
        Assert.True(PasswordHelper.VerifyPassword(TestHelpers.Password, stored.Password));
    }

    [Fact]
    public async Task Register_Organiser_WithoutInviteCode_Returns403()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Org", lastName = "Nizer", email = TestHelpers.UniqueEmail("org"), password = TestHelpers.Password,
            role = Roles.Organiser, companyName = "Co", registrationNumber = "REG-NOCODE-1",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongInviteCode_Returns403()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Org", lastName = "Nizer", email = TestHelpers.UniqueEmail("org"), password = TestHelpers.Password,
            role = Roles.Organiser, organiserInviteCode = "guessed", companyName = "Co", registrationNumber = "REG-BADCODE-1",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithValidInviteCode_CreatesOrganiserProfile()
    {
        var account = await _factory.CreateAccountAsync(Roles.Organiser);

        var hasProfile = await _factory.WithDbAsync(db => db.Organisers.AnyAsync(o => o.UserID == account.UserId));
        Assert.True(hasProfile);
    }

    [Fact]
    public async Task Register_Organiser_WithoutRegistrationNumber_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Org", lastName = "Nizer", email = TestHelpers.UniqueEmail("org"), password = TestHelpers.Password,
            role = Roles.Organiser, organiserInviteCode = RaceDayWebApplicationFactory.OrganiserInviteCode, companyName = "Co",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = TestHelpers.UniqueEmail("dupe");
        var body = new { firstName = "A", lastName = "B", email, password = TestHelpers.Password, role = Roles.Participant };

        var first = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", body);
        var second = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "A", lastName = "B", email = TestHelpers.UniqueEmail("role"), password = TestHelpers.Password, role = "Admin",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordShorterThanSixCharacters_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "A", lastName = "B", email = TestHelpers.UniqueEmail("short"), password = "abc", role = Roles.Participant,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200_AndSessionAuthorisesProtectedCalls()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var protectedCall = await account.Client.GetAsync($"/api/users/{account.UserId}");

        Assert.Equal(HttpStatusCode.OK, protectedCall.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = account.Email, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithLegacyPlainTextPassword_Returns401_NotServerError()
    {
        var email = TestHelpers.UniqueEmail("legacy");
        await _factory.WithDbAsync(async db =>
        {
            db.Users.Add(new User
            {
                Email = email, Password = "hashed_password_1", FirstName = "Legacy", LastName = "Row", UserRole = Roles.Participant,
                Participant = new Participant(),
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "hashed_password_1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutSession_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var logout = await account.Client.PostAsync("/api/auth/logout", null);
        var afterLogout = await account.Client.GetAsync($"/api/users/{account.UserId}");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RenewsSession_ForLoggedInUser_AndRejectsAnonymous()
    {
        var account = await _factory.CreateAccountAsync(Roles.Participant);

        var renewed = await account.Client.GetAsync("/api/auth/refresh-token");
        var anonymous = await _factory.CreateClient().GetAsync("/api/auth/refresh-token");

        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var json = await renewed.ReadJsonAsync();
        Assert.Equal(30, json.GetProperty("expiresInMinutes").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}