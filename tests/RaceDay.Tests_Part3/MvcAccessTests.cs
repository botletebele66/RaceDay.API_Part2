using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace RaceDay.Tests_Part3;

/// <summary>
/// Hosts the real MVC application in-process. The API is pointed at an address nothing listens on,
/// so these tests cover the MVC layer only: routing, access control and the login redirect.
/// </summary>
public sealed class MvcTestFactory : WebApplicationFactory<RaceDay.MVC.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:9/");
        builder.UseSetting("App:HttpsRedirect", "false");
    }
}

public class MvcAccessTests : IClassFixture<MvcTestFactory>
{
    private readonly WebApplicationFactory<RaceDay.MVC.Program> _factory;

    public MvcAccessTests(MvcTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientWithoutRedirects() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task LoginPage_IsPubliclyAvailable()
    {
        var response = await ClientWithoutRedirects().GetAsync("/Auth/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Events")]
    [InlineData("/Enrolments")]
    [InlineData("/Results")]
    [InlineData("/Profile")]
    [InlineData("/Dashboard")]
    public async Task ProtectedPages_RedirectAnonymousUsersToLogin(string path)
    {
        var response = await ClientWithoutRedirects().GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Auth/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Logout_IsNotAvailableAsAGetRequest()
    {
        var response = await ClientWithoutRedirects().GetAsync("/Auth/Logout");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
