using System.ComponentModel.DataAnnotations;

namespace RaceDay.API_Part2.DTOs;

/// <summary>Basic user details. Email is included only for the account owner.</summary>
/// <param name="UserId">User ID.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Role">"Organiser" or "Participant".</param>
/// <param name="Email">Email address (owner only, otherwise null).</param>
public record UserResponse(int UserId, string FirstName, string LastName, string Role, string? Email);

/// <summary>Role-specific profile returned to the account owner.</summary>
public record UserProfileResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? EmergencyContact,
    string? CompanyName,
    string? RegistrationNumber);

/// <summary>
/// Body for PUT /api/users/{id}. Account fields apply to every role. Participant fields
/// (DateOfBirth, EmergencyContact) apply to Participants; CompanyName and RegistrationNumber apply to Organisers.
/// Fields that do not apply to the account's role are ignored.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>First name.</summary>
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Email address. Must not belong to another account.</summary>
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional new password (at least 6 characters). Leave null to keep the current password.</summary>
    [StringLength(255, MinimumLength = 6)]
    public string? Password { get; set; }

    /// <summary>Contact number (both roles).</summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Participant: date of birth.</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Participant: emergency contact.</summary>
    [StringLength(255)]
    public string? EmergencyContact { get; set; }

    /// <summary>Organiser: company or club name (required for Organisers).</summary>
    [StringLength(255)]
    public string? CompanyName { get; set; }

    /// <summary>Organiser: registration number (required for Organisers, must be unique).</summary>
    [StringLength(50)]
    public string? RegistrationNumber { get; set; }
}
