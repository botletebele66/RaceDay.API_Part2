namespace RaceDay.API_Part2.DTOs
{
    public record RegisterDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role, // "Organiser" or "Participant"
                     // Organiser fields
        string? CompanyName,
        string? RegistrationNumber,
        // Participant fields
        DateTime? DateOfBirth,
        string? PhoneNumber,
        string? EmergencyContact
    );

    public record LoginDto(string Email, string Password);

}