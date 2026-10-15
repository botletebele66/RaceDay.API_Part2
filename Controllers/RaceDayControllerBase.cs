using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>
/// Shared session and ownership helpers, plus RFC 7807 problem responses. Problem responses are used
/// instead of Forbid()/Unauthorized() because the API has no authentication scheme registered.
/// </summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    protected RaceDayControllerBase(AppDbContext db) => Db = db;

    protected AppDbContext Db { get; }

    /// <summary>User ID from the session, or null when not logged in.</summary>
    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);

    protected bool IsCurrentUser(int userId) => CurrentUserId == userId;

    protected async Task<Organiser?> GetCurrentOrganiserAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return null;
        }
        return await Db.Organisers.AsNoTracking().FirstOrDefaultAsync(o => o.UserID == userId.Value, ct);
    }

    protected async Task<Participant?> GetCurrentParticipantAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return null;
        }
        return await Db.Participants.AsNoTracking().FirstOrDefaultAsync(p => p.UserID == userId.Value, ct);
    }

    protected IActionResult ProblemResponse(int statusCode, string title, string detail) =>
        Problem(detail: detail, title: title, statusCode: statusCode);

    protected IActionResult BadRequestProblem(string detail) => ProblemResponse(StatusCodes.Status400BadRequest, "Bad Request", detail);

    protected IActionResult ForbiddenProblem(string detail) => ProblemResponse(StatusCodes.Status403Forbidden, "Forbidden", detail);

    protected IActionResult NotFoundProblem(string detail) => ProblemResponse(StatusCodes.Status404NotFound, "Not Found", detail);

    protected IActionResult ConflictProblem(string detail) => ProblemResponse(StatusCodes.Status409Conflict, "Conflict", detail);

    protected IActionResult UnauthorizedProblem(string detail) => ProblemResponse(StatusCodes.Status401Unauthorized, "Unauthorized", detail);
}
