using RaceDay.MVC.Services;
using Xunit;

namespace RaceDay.Tests_Part3;

/// <summary>Display helpers used by the views. These are pure functions, so no host is needed.</summary>
public class UiFormatTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(112, "112th")]
    public void Ordinal_UsesEnglishSuffixes(int number, string expected)
    {
        Assert.Equal(expected, UiFormat.Ordinal(number));
    }

    [Fact]
    public void Duration_UnderAnHour_OmitsHours()
    {
        Assert.Equal("42:05", UiFormat.Duration(42 * 60 + 5));
    }

    [Fact]
    public void Duration_OverAnHour_IncludesHours()
    {
        Assert.Equal("1:02:03", UiFormat.Duration(3723));
    }

    [Fact]
    public void Duration_WhenMissing_ShowsDash()
    {
        Assert.Equal("—", UiFormat.Duration(null));
    }

    [Fact]
    public void Money_ZeroFee_IsFree()
    {
        Assert.Equal("Free", UiFormat.Money(0m));
    }

    [Fact]
    public void Money_PaidFee_IsPrefixedWithRand()
    {
        Assert.StartsWith("R ", UiFormat.Money(150m));
    }

    [Fact]
    public void Km_DropsTrailingZeros()
    {
        Assert.Equal("10 km", UiFormat.Km(10m));
        Assert.Equal("21.1 km", UiFormat.Km(21.10m));
    }

    [Fact]
    public void StatusLabel_RegisteredShowsAsConfirmed()
    {
        Assert.Equal("Confirmed", UiFormat.StatusLabel("Registered"));
    }

    [Theory]
    [InlineData("Registered", "status-confirmed")]
    [InlineData("Started", "status-pending")]
    [InlineData("Completed", "status-completed")]
    [InlineData("Withdrawn", "status-withdrawn")]
    public void StatusClass_MatchesColourGuide(string status, string expectedClass)
    {
        Assert.Equal(expectedClass, UiFormat.StatusClass(status));
    }
}
