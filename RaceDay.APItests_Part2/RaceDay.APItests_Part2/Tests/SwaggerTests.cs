using System.Net;
using System.Text.Json;
using RaceDay.APItests_Part2.Infrastructure;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

/// <summary>Checks that Swagger exposes every endpoint from the Part 1 plan (requirement 4).</summary>
public class SwaggerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public SwaggerTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    public static IEnumerable<object[]> PlannedEndpoints() => new List<object[]>
    {
        new object[] { "/api/auth/register", "post" },
        new object[] { "/api/auth/login", "post" },
        new object[] { "/api/auth/logout", "post" },
        new object[] { "/api/auth/refresh-token", "get" },
        new object[] { "/api/users/{id}", "get" },
        new object[] { "/api/users/{id}", "put" },
        new object[] { "/api/users/{id}/profile", "get" },
        new object[] { "/api/events", "get" },
        new object[] { "/api/events", "post" },
        new object[] { "/api/events/{id}", "get" },
        new object[] { "/api/events/{id}", "put" },
        new object[] { "/api/events/{id}", "delete" },
        new object[] { "/api/events/{eventId}/categories", "get" },
        new object[] { "/api/events/{eventId}/categories", "post" },
        new object[] { "/api/categories/{id}", "put" },
        new object[] { "/api/categories/{id}", "delete" },
        new object[] { "/api/enrolments", "get" },
        new object[] { "/api/enrolments", "post" },
        new object[] { "/api/enrolments/{id}", "delete" },
        new object[] { "/api/events/{eventId}/enrolments", "get" },
        new object[] { "/api/results", "post" },
        new object[] { "/api/results/{enrolmentId}", "get" },
        new object[] { "/api/participants/{id}/history", "get" },
    };

    [Fact]
    public async Task EveryOperation_HasASummary_AndDescribedResponses()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        var document = await response.ReadJsonAsync();

        var problems = new List<string>();
        foreach (var path in document.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var name = $"{operation.Name.ToUpperInvariant()} {path.Name}";
                if (!operation.Value.TryGetProperty("summary", out var summary) || string.IsNullOrWhiteSpace(summary.GetString()))
                {
                    problems.Add($"{name}: no summary (is RaceDay.API_Part2.xml next to the API assembly?)");
                }
                if (!operation.Value.TryGetProperty("responses", out var responses) || !responses.EnumerateObject().Any())
                {
                    problems.Add($"{name}: no documented responses");
                    continue;
                }
                foreach (var code in responses.EnumerateObject())
                {
                    if (!code.Value.TryGetProperty("description", out var description) || string.IsNullOrWhiteSpace(description.GetString()))
                    {
                        problems.Add($"{name}: response {code.Name} has no description");
                    }
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PlannedEndpoints))]
    public async Task SwaggerDocument_ContainsPlannedEndpoint(string path, string method)
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.ReadJsonAsync();
        var paths = document.GetProperty("paths");
        Assert.True(paths.TryGetProperty(path, out var operations), $"Swagger is missing {path}");
        Assert.True(operations.TryGetProperty(method, out _), $"Swagger is missing {method.ToUpperInvariant()} {path}");
    }
}
