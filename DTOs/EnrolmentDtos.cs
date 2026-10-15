using System.ComponentModel.DataAnnotations;

namespace RaceDay.API_Part2.DTOs;

/// <summary>Body for POST /api/enrolments.</summary>
public class EnrolRequest
{
    /// <summary>ID of the category to enter.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>Response after a successful enrolment.</summary>
public record EnrolmentCreatedResponse(int EnrolmentId, string Message);

/// <summary>A participant's enrolment, with event, category and result (when recorded).</summary>
public record EnrolmentSummaryResponse(
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
    int? PlacementRank);

/// <summary>An enrolment as seen by the organiser of the event.</summary>
public record EventEnrolmentResponse(
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
