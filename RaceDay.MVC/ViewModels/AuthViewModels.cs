using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 6, ErrorMessage = "Use at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>"Participant" or "Organiser".</summary>
    [Required]
    public string Role { get; set; } = "Participant";

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(255)]
    [Display(Name = "Emergency contact")]
    public string? EmergencyContact { get; set; }

    [StringLength(255)]
    [Display(Name = "Company or club name")]
    public string? CompanyName { get; set; }

    [StringLength(50)]
    [Display(Name = "Registration number")]
    public string? RegistrationNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Organiser invite code")]
    public string? OrganiserInviteCode { get; set; }
}
