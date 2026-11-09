using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.ViewModels;

/// <summary>One distance or age category being added to an event.</summary>
public sealed class CategoryInput
{
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; } = 10;

    [Range(0, 100000, ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? AgeRestriction { get; set; }
}

/// <summary>Create and edit form for an event. The banner is optional.</summary>
public sealed class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required, StringLength(255)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the start date and time.")]
    [Display(Name = "Start date and time")]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = EventTypes.Running;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100 000.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }

    /// <summary>Categories added with the event (create only). Each one is a distance or age group.</summary>
    public List<CategoryInput> Categories { get; set; } = [];

    /// <summary>Banner image to upload (JPG, PNG, WEBP or GIF, up to 5 MB).</summary>
    [Display(Name = "Event banner")]
    public IFormFile? Banner { get; set; }

    public string? CurrentImageUrl { get; set; }
}

public sealed record OrganiserEventRow(EventDto Event, int EnrolmentCount);

public sealed class OrganiserEventsViewModel
{
    public IReadOnlyList<OrganiserEventRow> Rows { get; init; } = [];
}

public sealed class EventManageViewModel
{
    public required EventDto Event { get; init; }
    public IReadOnlyList<EventEnrolmentDto> Enrolments { get; init; } = [];
}

public sealed class EventEnrolmentsViewModel
{
    public required EventDto Event { get; init; }
    public IReadOnlyList<EventEnrolmentDto> Enrolments { get; init; } = [];
}

/// <summary>One row of the results grid: an enrolment, its existing result (if any) and the values being entered.</summary>
public sealed class ResultRowViewModel
{
    public int EnrolmentId { get; init; }
    public string ParticipantName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public ResultDto? Existing { get; init; }
}

public sealed class ResultsEntryViewModel
{
    public required EventDto Event { get; init; }
    public List<ResultRowViewModel> Rows { get; init; } = [];
}

/// <summary>Posted from the results grid. Rows are submitted by index, and a row with no values is skipped.</summary>
public sealed class RecordResultsInput
{
    public int EventId { get; set; }
    public List<ResultRowInput> Rows { get; set; } = [];
}

public sealed class ResultRowInput
{
    public int EnrolmentId { get; set; }
    public string? Name { get; set; }
    public int? Hours { get; set; }
    public int? Minutes { get; set; }
    public int? Seconds { get; set; }
    public int? Position { get; set; }
}
