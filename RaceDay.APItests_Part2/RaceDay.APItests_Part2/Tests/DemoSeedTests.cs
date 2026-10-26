using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.Helper;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

/// <summary>Checks the Development demo data loaded by <see cref="DbInitializer"/>.</summary>
public class DemoSeedTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void DemoSeed_LoadsEveryAccount_Event_Category_Enrolment_AndResult()
    {
        using var db = NewContext();

        DbInitializer.Initialize(db, seedDemoData: true);

        Assert.Equal(4, db.Users.Count());
        Assert.Equal(2, db.Organisers.Count());
        Assert.Equal(2, db.Participants.Count());
        Assert.Equal(4, db.Events.Count());
        Assert.Equal(6, db.EventCategories.Count());   // includes the Cape Town Cycle Tour categories, which no enrolment points at
        Assert.Equal(5, db.Enrolments.Count());
        Assert.Equal(2, db.Results.Count());
    }

    [Fact]
    public void DemoSeed_GivesEveryEventAtLeastOneCategory()
    {
        using var db = NewContext();

        DbInitializer.Initialize(db, seedDemoData: true);

        Assert.True(db.Events.All(e => e.Categories.Any()), "An event was seeded without categories.");
    }

    [Fact]
    public void DemoSeed_StoresPasswordsAsHashes_AndTheDemoPasswordVerifies()
    {
        using var db = NewContext();

        DbInitializer.Initialize(db, seedDemoData: true);

        var user = db.Users.First();
        Assert.NotEqual(DbInitializer.DemoPassword, user.Password);
        Assert.True(PasswordHelper.VerifyPassword(DbInitializer.DemoPassword, user.Password));
    }

    [Fact]
    public void DemoSeed_IsSkipped_WhenDisabled()
    {
        using var db = NewContext();

        DbInitializer.Initialize(db, seedDemoData: false);

        Assert.Equal(0, db.Users.Count());
    }

    [Fact]
    public void DemoSeed_DoesNotRunTwice()
    {
        using var db = NewContext();

        DbInitializer.Initialize(db, seedDemoData: true);
        DbInitializer.Initialize(db, seedDemoData: true);

        Assert.Equal(4, db.Users.Count());
        Assert.Equal(4, db.Events.Count());
    }
}
