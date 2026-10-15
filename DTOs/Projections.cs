using System.Linq.Expressions;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.DTOs;

/// <summary>
/// EF Core projections shared by the controllers. Each query selects straight into a response record,
/// so no entity graph is loaded or serialised.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<EventCategory, CategoryResponse>> CategoryProjection = c =>
        new CategoryResponse(c.CategoryID, c.EventID, c.CategoryName, c.Distance, c.EntryFee, c.AgeRestriction);

    public static readonly Expression<Func<Event, EventResponse>> EventProjection = e =>
        new EventResponse(
            e.EventID,
            e.EventName,
            e.EventDate,
            e.Location,
            e.EventType,
            e.Description,
            e.MaxParticipants,
            e.Categories
                .OrderBy(c => c.CategoryID)
                .Select(c => new CategoryResponse(c.CategoryID, c.EventID, c.CategoryName, c.Distance, c.EntryFee, c.AgeRestriction))
                .ToList());

    public static readonly Expression<Func<Enrolment, EnrolmentSummaryResponse>> EnrolmentSummaryProjection = e =>
        new EnrolmentSummaryResponse(
            e.EnrolmentID,
            e.Status,
            e.EnrolmentDate,
            e.Category.EventID,
            e.Category.Event.EventName,
            e.Category.Event.EventDate,
            e.Category.Event.Location,
            e.Category.Event.EventType,
            e.CategoryID,
            e.Category.CategoryName,
            e.Category.Distance,
            e.Result == null ? null : e.Result.TimeCompleted,
            e.Result == null ? null : e.Result.PlacementRank);

    public static readonly Expression<Func<Enrolment, EventEnrolmentResponse>> EventEnrolmentProjection = e =>
        new EventEnrolmentResponse(
            e.EnrolmentID,
            e.Status,
            e.EnrolmentDate,
            e.ParticipantID,
            e.Participant.User.FirstName,
            e.Participant.User.LastName,
            e.Participant.User.Email,
            e.CategoryID,
            e.Category.CategoryName,
            e.Category.Distance);

    public static readonly Expression<Func<Result, ResultResponse>> ResultProjection = r =>
        new ResultResponse(r.ResultID, r.EnrolmentID, r.TimeCompleted, r.PlacementRank, r.DateCompleted);
}
