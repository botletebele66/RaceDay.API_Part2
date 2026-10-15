namespace RaceDay.API_Part2.Helper;

/// <summary>BCrypt password hashing. Plain-text passwords are never stored.</summary>
public static class PasswordHelper
{
    /// <summary>Minimum password length, as specified in the Part 1 registration design.</summary>
    public const int MinimumLength = 6;

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// Returns true only when the password matches the stored hash. A malformed stored value
    /// (for example a legacy plain-text seed row) is treated as a failed match rather than an error.
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
