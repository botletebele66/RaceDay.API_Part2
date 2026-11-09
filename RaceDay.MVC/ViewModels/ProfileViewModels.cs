using System.ComponentModel.DataAnnotations;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.ViewModels;

public sealed class ProfileFormViewModel
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

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

    /// <summary>Leave blank to keep the current password.</summary>
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Use at least 6 characters.")]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; set; }

    public bool IsOrganiser => Role == Roles.Organiser;
}

public sealed class DashboardViewModel
{
    // Organiser figures
    public int EventCount { get; init; }
    public int UpcomingEventCount { get; init; }
    public int TotalEnrolments { get; init; }
    public IReadOnlyList<OrganiserEventRow> EventRows { get; init; } = [];

    // Participant figures
    public int UpcomingEnrolmentCount { get; init; }
    public int CompletedCount { get; init; }
    public IReadOnlyList<EnrolmentDto> UpcomingEnrolments { get; init; } = [];
}

public sealed class EventBrowseViewModel
{
    public IReadOnlyList<EventDto> Events { get; init; } = [];
    public string EventType { get; init; } = "All";
    public string? Query { get; init; }
    public string When { get; init; } = "upcoming";
    public int TotalCategories { get; init; }
}

public sealed class EventDetailsViewModel
{
    public required EventDto Event { get; init; }
    public bool IsOwner { get; init; }
    public IReadOnlySet<int> EnrolledCategoryIds { get; init; } = new HashSet<int>();
    public bool IsEventOver { get; init; }
}

public sealed class MyEventsViewModel
{
    public IReadOnlyList<EnrolmentDto> Enrolments { get; init; } = [];
}

public sealed class MyResultsViewModel
{
    public IReadOnlyList<EnrolmentDto> Results { get; init; } = [];
}
