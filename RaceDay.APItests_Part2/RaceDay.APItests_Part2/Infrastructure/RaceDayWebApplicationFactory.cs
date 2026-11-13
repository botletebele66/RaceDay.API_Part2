using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API_Part2;
using RaceDay.API_Part2.Data;

namespace RaceDay.APItests_Part2.Infrastructure;

/// <summary>
/// Runs the real API in-process against an EF Core in-memory database. Each test class gets its own factory
/// (and therefore its own database), so data never leaks between classes.
/// The "Testing" environment skips demo seeding and HTTPS redirection.
/// </summary>
public sealed class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Organiser invite code configured for the test host.</summary>
    public const string OrganiserInviteCode = "test-invite-code";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Registration:OrganiserInviteCode", OrganiserInviteCode);
        builder.ConfigureServices(services =>
        {
            // EF Core 9+ keeps provider setup in IDbContextOptionsConfiguration<T> as well as DbContextOptions<T>.
            // Both must be removed, otherwise SQL Server and the in-memory provider are registered together and EF throws.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }
}
