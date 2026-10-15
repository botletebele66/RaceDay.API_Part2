using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Participant enrolments in event categories, and the organiser's view of enrolments for their events.</summary>
[ApiController]
[Route("api")]
public class EnrolmentsController : RaceDayControllerBase
{
    public EnrolmentsController(AppDbContext db) : base(db) { }

    /// <summary>List your own enrolments, including withdrawn ones and any results.</summary>
    /// <response code="200">Enrolments, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an organiser.</response>
    [HttpGet("enrolments")]
    [AuthorizeRole(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments(CancellationToken ct)
    {
        var participant = await GetCurrentParticipantAsync(ct);
        if (participant is null)
        {
            return ForbiddenProblem("Only participants have enrolments.");
        }

        var enrolments = await Db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantID == participant.ParticipantID)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .Select(Projections.EnrolmentSummaryProjection)
            .ToListAsync(ct);

        return Ok(enrolments);
    }

    /// <summary>Enrol in an event by choosing one of its categories.</summary>
    /// <remarks>
    /// Rules: the event must not have started; you cannot enrol twice in the same category; age-restricted categories
    /// need a date of birth on your profile and an age on the event date at or above the restriction; and the event
    /// capacity (MaxParticipants) must not be exceeded. A previously withdrawn enrolment in the same category is
    /// reactivated rather than duplicated.
    /// </remarks>
    /// <response code="201">Enrolled.</response>
    /// <response code="400">Age restriction not met, or date of birth missing for an age-restricted category.</response>
    /// <response code="403">Caller is not a participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled, event started, or event full.</response>
    [HttpPost("enrolments")]
    [AuthorizeRole(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol([FromBody] EnrolRequest request, CancellationToken ct)
    {
        var participant = await GetCurrentParticipantAsync(ct);
        if (participant is null)
        {
            return ForbiddenProblem("Only participants can enrol in events.");
        }

        var category = await Db.EventCategories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId, ct);
        if (category is null)
        {
            return NotFoundProblem("Category not found.");
        }

        var ev = category.Event;
        if (ev.EventDate <= DateTime.UtcNow)
        {
            return ConflictProblem("Enrolment is closed because the event has already started or taken place.");
        }

        var existing = await Db.Enrolments.FirstOrDefaultAsync(
            e => e.ParticipantID == participant.ParticipantID && e.CategoryID == category.CategoryID, ct);
        if (existing is not null && existing.Status != EnrolmentStatuses.Withdrawn)
        {
            return ConflictProblem("You are already enrolled in this category.");
        }

        if (category.AgeRestriction is int minimumAge)
        {
            if (participant.DateOfBirth is null)
            {
                return BadRequestProblem("Add your date of birth to your profile to enter an age-restricted category.");
            }

            var ageOnEventDate = AgeHelper.AgeOn(participant.DateOfBirth.Value, ev.EventDate);
            if (ageOnEventDate < minimumAge)
            {
                return BadRequestProblem($"This category requires participants to be at least {minimumAge} years old on the event date.");
            }
        }

        // Serializable isolation stops two concurrent enrolments from both taking the last place.
        // Non-relational providers (the in-memory provider used by the tests) do not support transactions.
        await using var transaction = Db.Database.IsRelational()
            ? await Db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        if (ev.MaxParticipants is int capacity)
        {
            var placesTaken = await Db.Enrolments.CountAsync(
                e => e.Category.EventID == ev.EventID && e.Status != EnrolmentStatuses.Withdrawn, ct);
            if (placesTaken >= capacity)
            {
                return ConflictProblem("This event is full.");
            }
        }

        Enrolment enrolment;
        if (existing is not null)
        {
            existing.Status = EnrolmentStatuses.Registered;
            existing.EnrolmentDate = DateTime.UtcNow;
            existing.UpdatedDate = DateTime.UtcNow;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                CategoryID = category.CategoryID,
                Status = EnrolmentStatuses.Registered,
            };
            Db.Enrolments.Add(enrolment);
        }

        await Db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        return Created("/api/enrolments", new EnrolmentCreatedResponse(enrolment.EnrolmentID, "Enrolled successfully."));
    }

    /// <summary>Withdraw from an event (your own enrolment).</summary>
    /// <remarks>
    /// The enrolment is kept with status Withdrawn so history is preserved. It is not allowed once the event has started,
    /// once a result exists, or if it is already withdrawn.
    /// </remarks>
    /// <response code="204">Withdrawn.</response>
    /// <response code="403">The enrolment belongs to another participant, or caller is not a participant.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Already withdrawn, completed, or the event has started.</response>
    [HttpDelete("enrolments/{id:int}")]
    [AuthorizeRole(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id, CancellationToken ct)
    {
        var participant = await GetCurrentParticipantAsync(ct);
        if (participant is null)
        {
            return ForbiddenProblem("Only participants can withdraw from events.");
        }

        var enrolment = await Db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id, ct);
        if (enrolment is null)
        {
            return NotFoundProblem("Enrolment not found.");
        }

        if (enrolment.ParticipantID != participant.ParticipantID)
        {
            return ForbiddenProblem("You can only withdraw from your own enrolments.");
        }

        if (enrolment.Status == EnrolmentStatuses.Withdrawn)
        {
            return ConflictProblem("This enrolment has already been withdrawn.");
        }

        if (enrolment.Result is not null || enrolment.Status == EnrolmentStatuses.Completed)
        {
            return ConflictProblem("You cannot withdraw from an event after a result has been recorded.");
        }

        if (enrolment.Category.Event.EventDate <= DateTime.UtcNow)
        {
            return ConflictProblem("You cannot withdraw once the event has started.");
        }

        enrolment.Status = EnrolmentStatuses.Withdrawn;
        enrolment.UpdatedDate = DateTime.UtcNow;
        await Db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>List every enrolment for an event you organise.</summary>
    /// <remarks>Includes withdrawn enrolments, marked with their status. Participant names and email addresses are returned.</remarks>
    /// <response code="200">Enrolments for the event.</response>
    /// <response code="403">Event belongs to another organiser, or the caller is not an organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EventEnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId, CancellationToken ct)
    {
        var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId, ct);
        if (ev is null)
        {
            return NotFoundProblem("Event not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || ev.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only view enrolments for events that you organise.");
        }

        var enrolments = await Db.Enrolments.AsNoTracking()
            .Where(e => e.Category.EventID == eventId)
            .OrderBy(e => e.EnrolmentID)
            .Select(Projections.EventEnrolmentProjection)
            .ToListAsync(ct);

        return Ok(enrolments);
    }
}
