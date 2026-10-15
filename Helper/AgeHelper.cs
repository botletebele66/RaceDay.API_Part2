namespace RaceDay.API_Part2.Helper;

/// <summary>Age calculations used for age-restricted event categories.</summary>
public static class AgeHelper
{
    /// <summary>Completed years between <paramref name="birthDate"/> and <paramref name="onDate"/>.</summary>
    public static int AgeOn(DateTime birthDate, DateTime onDate)
    {
        var age = onDate.Year - birthDate.Year;
        if (onDate.Month < birthDate.Month || (onDate.Month == birthDate.Month && onDate.Day < birthDate.Day))
        {
            age--;
        }
        return age;
    }
}
