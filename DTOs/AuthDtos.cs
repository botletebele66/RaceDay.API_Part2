using System.ComponentModel.DataAnnotations;

namespace RaceDay.API_Part2.DTOs;

/// <summary>Body for POST /api/auth/register.</summary>
public class RegisterRequest
{
    /// <summary>First name (required).</summary>
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name (required).</summary>
    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Login email address. Must be unique; stored lower-case.</summary>
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Plain-text password, at least 6 characters. Stored only as a BCrypt hash.</summary>
    [Required, StringLength(255, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>"Organiser" or "Participant".</summary>
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Required when Role is Organiser. Issued by the system administrator (Registration:OrganiserInviteCode).</summary>
    [StringLength(100)]
    public string? OrganiserInviteCode { get; set; }

    /// <summary>Required for Organisers: company or club name.</summary>
    [StringLength(255)]
    public string? CompanyName { get; set; }

    /// <summary>Required for Organisers: unique registration number.</summary>
    [StringLength(50)]
    public string? RegistrationNumber { get; set; }

    /// <summary>Optional for Participants. Needed to enter age-restricted categories.</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Optional contact number.</summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Optional for Participants: emergency contact name and number.</summary>
    [StringLength(255)]
    public string? EmergencyContact { get; set; }
}

/// <summary>Body for POST /api/auth/login.</summary>
public class LoginRequest
{
    /// <summary>Registered email address.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Response for GET /api/auth/refresh-token.</summary>
/// <param name="User">The logged-in user.</param>
/// <param name="ExpiresInMinutes">Idle timeout; the session is renewed on each call to this endpoint.</param>
public record SessionResponse(UserResponse User, int ExpiresInMinutes);
