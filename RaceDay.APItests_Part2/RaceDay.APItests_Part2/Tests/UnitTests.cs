using RaceDay.API_Part2.Helper;
using Xunit;

namespace RaceDay.APItests_Part2.Tests;

/// <summary>Pure unit tests for the helper classes. No HTTP host or database is needed.</summary>
public class UnitTests
{
    [Fact]
    public void HashPassword_ProducesDifferentHashesForTheSamePassword()
    {
        var first = PasswordHelper.HashPassword("Secret123");
        var second = PasswordHelper.HashPassword("Secret123");

        Assert.NotEqual(first, second);
        Assert.True(PasswordHelper.VerifyPassword("Secret123", first));
        Assert.True(PasswordHelper.VerifyPassword("Secret123", second));
    }

    [Fact]
    public void VerifyPassword_WithMalformedStoredValue_ReturnsFalse()
    {
        Assert.False(PasswordHelper.VerifyPassword("anything", "hashed_password_1"));
    }

    [Fact]
    public void AgeOn_TheDayBeforeBirthday_IsOneYearYounger()
    {
        Assert.Equal(17, AgeHelper.AgeOn(new DateTime(2000, 6, 15), new DateTime(2018, 6, 14)));
    }

    [Fact]
    public void AgeOn_OnBirthday_IsTheNewAge()
    {
        Assert.Equal(18, AgeHelper.AgeOn(new DateTime(2000, 6, 15), new DateTime(2018, 6, 15)));
    }

    [Fact]
    public void AgeOn_LeapDayBirthday_OnNonLeapYear_IsZeroBeforeMarch1()
    {
        Assert.Equal(0, AgeHelper.AgeOn(new DateTime(2000, 2, 29), new DateTime(2001, 2, 28)));
    }

    [Fact]
    public void ToUtc_TreatsUnspecifiedAsUtc()
    {
        var result = TimeHelper.ToUtc(new DateTime(2026, 12, 1, 8, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(8, result.Hour);
    }

    [Fact]
    public void ToUtc_ConvertsLocalTimeToUtc()
    {
        var local = DateTime.SpecifyKind(new DateTime(2026, 12, 1, 8, 0, 0), DateTimeKind.Local);

        Assert.Equal(DateTimeKind.Utc, TimeHelper.ToUtc(local).Kind);
    }

    [Fact]
    public void Constants_MatchThePart1SchemaCheckConstraints()
    {
        Assert.Equal(new[] { "Running", "Walking", "Cycling" }, EventTypes.All);
        Assert.Equal(new[] { "Organiser", "Participant" }, Roles.All);
        Assert.Contains(EnrolmentStatuses.Withdrawn, new[] { "Registered", "Started", "Completed", "Withdrawn" });
    }
}
