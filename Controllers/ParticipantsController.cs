using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Participant race history.</summary>
[ApiController]
[Route("api/participants")]
public class ParticipantsController : RaceDayControllerBase
{
    public ParticipantsController(AppDbContext db) : base(db) { }

    /// <summary>Get a participant's race history.</summary>
    /// <remarks>
    /// The participant sees their full history. An organiser sees only the enrolments in their own events.
    /// Any other user receives an empty list.
    /// </remarks>
    /// <param name="id">ParticipantID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">History, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is neither the participant nor an organiser.</response>
    /// <response code="404">Participant not found.</response>
    [HttpGet("{id:int}/history")]
    [AuthorizeRole]
    [ProducesResponseType(typeof(List<EnrolmentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
    {
        var participant = await Db.Participants.AsNoTracking().FirstOrDefaultAsync(p => p.ParticipantID == id, ct);
        if (participant is null)
        {
            return NotFoundProblem("Participant not found.");
        }

        IQueryable<Enrolment> query = Db.Enrolments.AsNoTracking().Where(e => e.ParticipantID == id);

        if (!IsCurrentUser(participant.UserID))
        {
            var organiser = await GetCurrentOrganiserAsync(ct);
            if (organiser is null)
            {
                return ForbiddenProblem("Only the participant or an organiser can view this history.");
            }

            var organiserId = organiser.OrganiserID;
            query = query.Where(e => e.Category.Event.OrganiserID == organiserId);
        }

        var history = await query
            .OrderByDescending(e => e.Category.Event.EventDate)
            .Select(Projections.EnrolmentSummaryProjection)
            .ToListAsync(ct);

        return Ok(history);
    }
}
