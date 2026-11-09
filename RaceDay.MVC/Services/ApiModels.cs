namespace RaceDay.MVC.Services;

// These records mirror the API's response DTOs. Property names are matched case-insensitively against the API's camelCase JSON.

/// <summary>The signed-in user (login, refresh and registration responses).</summary>
public sealed record UserDto(int UserId, string FirstName, string LastName, string Role, string? Email);

/// <summary>The account owner's full profile.</summary>
public sealed record UserProfileDto(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? EmergencyContact,
    string? CompanyName,
    string? RegistrationNumber,
    string? ProfileImageUrl);

public sealed record CategoryDto(int CategoryId, int EventId, string CategoryName, decimal Distance, decimal EntryFee, int? AgeRestriction);

/// <summary>An event with its categories. Dates are UTC, as stored by the API.</summary>
public sealed record EventDto(
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    string EventType,
    string? Description,
    int? MaxParticipants,
    List<CategoryDto> Categories,
    int OrganiserUserId,
    string? ImageUrl);

/// <summary>A participant's enrolment, with its event, category and any recorded result.</summary>
public sealed record EnrolmentDto(
    int EnrolmentId,
    string Status,
    DateTime EnrolmentDate,
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    string EventType,
    int CategoryId,
    string CategoryName,
    decimal Distance,
    int? TimeCompleted,
    int? PlacementRank,
    int? TotalFinishers);

/// <summary>An enrolment as the organiser sees it.</summary>
public sealed record EventEnrolmentDto(
    int EnrolmentId,
    string Status,
    DateTime EnrolmentDate,
    int ParticipantId,
    string FirstName,
    string LastName,
    string Email,
    int CategoryId,
    string CategoryName,
    decimal Distance);

public sealed record ResultDto(int ResultId, int EnrolmentId, int? TimeCompleted, int? PlacementRank, DateTime DateCompleted);

public sealed record EnrolmentCreatedDto(int EnrolmentId, string Message);
