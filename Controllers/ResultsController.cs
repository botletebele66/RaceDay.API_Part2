using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Race results. Organisers record them after an event; participants and organisers can view them.</summary>
[ApiController]
[Route("api/results")]
public class ResultsController : RaceDayControllerBase
{
    public ResultsController(AppDbContext db) : base(db) { }

    /// <summary>Record the finish time and position for an enrolment in an event you organise.</summary>
    /// <remarks>
    /// Only allowed after the event date, once per enrolment, and not for withdrawn enrolments. The enrolment status changes to Completed.
    /// </remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Event belongs to another organiser, or the caller is not an organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Event has not taken place, enrolment withdrawn, or result already recorded.</response>
    [HttpPost]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordResult([FromBody] RecordResultRequest request, CancellationToken ct)
    {
        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null)
        {
            return ForbiddenProblem("Only organisers can record results.");
        }

        var enrolment = await Db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentId, ct);
        if (enrolment is null)
        {
            return NotFoundProblem("Enrolment not found.");
        }

        if (enrolment.Category.Event.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only record results for events that you organise.");
        }

        if (enrolment.Status == EnrolmentStatuses.Withdrawn)
        {
            return ConflictProblem("Withdrawn enrolments cannot receive a result.");
        }

        if (enrolment.Category.Event.EventDate > DateTime.UtcNow)
        {
            return ConflictProblem("Results can only be recorded after the event has taken place.");
        }

        if (enrolment.Result is not null)
        {
            return ConflictProblem("A result has already been recorded for this enrolment.");
        }

        var now = DateTime.UtcNow;
        var result = new Result
        {
            EnrolmentID = enrolment.EnrolmentID,
            TimeCompleted = request.TimeCompletedSeconds,
            PlacementRank = request.PlacementRank,
            DateCompleted = now,
        };

        enrolment.Status = EnrolmentStatuses.Completed;
        enrolment.UpdatedDate = now;
        Db.Results.Add(result);

        await Db.SaveChangesAsync(ct);

        return Created($"/api/results/{enrolment.EnrolmentID}",
            new ResultResponse(result.ResultID, result.EnrolmentID, result.TimeCompleted, result.PlacementRank, result.DateCompleted));
    }

    /// <summary>Get the result for an enrolment.</summary>
    /// <remarks>Visible to the participant who owns the enrolment and to the organiser of that event. Anyone else receives 403.</remarks>
    /// <response code="200">Result found.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the participant or the organiser for this enrolment.</response>
    /// <response code="404">Enrolment not found, or no result recorded yet.</response>
    [HttpGet("{enrolmentId:int}")]
    [AuthorizeRole]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(int enrolmentId, CancellationToken ct)
    {
        var enrolment = await Db.Enrolments.AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId, ct);
        if (enrolment is null)
        {
            return NotFoundProblem("Enrolment not found.");
        }

        var isOwningParticipant = IsCurrentUser(enrolment.Participant.UserID);
        var isEventOrganiser = false;
        if (!isOwningParticipant)
        {
            var organiser = await GetCurrentOrganiserAsync(ct);
            isEventOrganiser = organiser is not null && organiser.OrganiserID == enrolment.Category.Event.OrganiserID;
        }

        if (!isOwningParticipant && !isEventOrganiser)
        {
            return ForbiddenProblem("You can only view results for your own enrolments or for events you organise.");
        }

        if (enrolment.Result is null)
        {
            return NotFoundProblem("No result has been recorded for this enrolment yet.");
        }

        var result = enrolment.Result;
        return Ok(new ResultResponse(result.ResultID, result.EnrolmentID, result.TimeCompleted, result.PlacementRank, result.DateCompleted));
    }
}
