using System.ComponentModel.DataAnnotations;

namespace RaceDay.API_Part2.DTOs;

/// <summary>Body for POST /api/results. Recorded by the organiser after the event.</summary>
public class RecordResultRequest
{
    /// <summary>The enrolment the result belongs to.</summary>
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time in whole seconds (greater than 0).</summary>
    [Range(1, int.MaxValue)]
    public int TimeCompletedSeconds { get; set; }

    /// <summary>Finishing position (1 is first).</summary>
    [Range(1, int.MaxValue)]
    public int PlacementRank { get; set; }
}

/// <summary>A recorded race result.</summary>
public record ResultResponse(int ResultId, int EnrolmentId, int? TimeCompleted, int? PlacementRank, DateTime DateCompleted);
