using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Browse events (public). Organisers create, edit and delete their own events.</summary>
[ApiController]
[Route("api/events")]
public class EventsController : RaceDayControllerBase
{
    public EventsController(AppDbContext db) : base(db) { }

    /// <summary>List events (public).</summary>
    /// <remarks>Each event includes its categories. Optional filters: eventType, and upcomingOnly to hide events that have started.</remarks>
    /// <param name="eventType">Running, Walking or Cycling.</param>
    /// <param name="upcomingOnly">When true, only events whose date is in the future are returned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Events, ordered by date.</response>
    /// <response code="400">eventType is not a recognised value.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents([FromQuery] string? eventType, [FromQuery] bool upcomingOnly = false, CancellationToken ct = default)
    {
        IQueryable<Event> query = Db.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (!EventTypes.All.Contains(eventType))
            {
                return BadRequestProblem($"eventType must be one of: {string.Join(", ", EventTypes.All)}.");
            }
            query = query.Where(e => e.EventType == eventType);
        }

        if (upcomingOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(e => e.EventDate > now);
        }

        var events = await query.OrderBy(e => e.EventDate).Select(Projections.EventProjection).ToListAsync(ct);
        return Ok(events);
    }

    /// <summary>Get one event with its categories (public).</summary>
    /// <response code="200">Event found.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvent(int id, CancellationToken ct)
    {
        var ev = await Db.Events.AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(Projections.EventProjection)
            .FirstOrDefaultAsync(ct);

        return ev is null ? NotFoundProblem("Event not found.") : Ok(ev);
    }

    /// <summary>Create an event owned by the logged-in organiser.</summary>
    /// <remarks>
    /// The event date must be in the future. Categories may be supplied now or added later. Category names must be unique within the event.
    /// </remarks>
    /// <response code="201">Event created. Returns the event with its categories.</response>
    /// <response code="400">Validation failed (for example a past date or duplicate category names).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a participant, or the organiser profile is missing.</response>
    [HttpPost]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null)
        {
            return ForbiddenProblem("Your account is not registered as an organiser.");
        }

        if (string.IsNullOrWhiteSpace(request.EventName) || string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequestProblem("Event name and location cannot be blank.");
        }

        var eventDate = TimeHelper.ToUtc(request.EventDate);
        if (eventDate <= DateTime.UtcNow)
        {
            return BadRequestProblem("Event date must be in the future.");
        }

        var duplicate = request.Categories
            .GroupBy(c => c.CategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return BadRequestProblem($"Category name '{duplicate.Key}' is used more than once.");
        }

        var ev = new Event
        {
            OrganiserID = organiser.OrganiserID,
            EventName = request.EventName.Trim(),
            EventDate = eventDate,
            Location = request.Location.Trim(),
            EventType = request.EventType,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            MaxParticipants = request.MaxParticipants,
        };

        foreach (var category in request.Categories)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return BadRequestProblem("Category names cannot be blank.");
            }

            ev.Categories.Add(new EventCategory
            {
                CategoryName = category.CategoryName.Trim(),
                Distance = category.Distance,
                EntryFee = category.EntryFee,
                AgeRestriction = category.AgeRestriction,
            });
        }

        Db.Events.Add(ev);
        await Db.SaveChangesAsync(ct);

        var response = await Db.Events.AsNoTracking()
            .Where(e => e.EventID == ev.EventID)
            .Select(Projections.EventProjection)
            .SingleAsync(ct);

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, response);
    }

    /// <summary>Update an event that you organise.</summary>
    /// <remarks>
    /// A new date must be in the future. Capacity cannot be set below the number of active (non-withdrawn) enrolments.
    /// </remarks>
    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Capacity is below the current number of enrolments.</response>
    [HttpPut("{id:int}")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == id, ct);
        if (ev is null)
        {
            return NotFoundProblem("Event not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || ev.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only manage events that you organise.");
        }

        if (string.IsNullOrWhiteSpace(request.EventName) || string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequestProblem("Event name and location cannot be blank.");
        }

        var eventDate = TimeHelper.ToUtc(request.EventDate);
        if (eventDate != ev.EventDate && eventDate <= DateTime.UtcNow)
        {
            return BadRequestProblem("A new event date must be in the future.");
        }

        if (request.MaxParticipants is int capacity)
        {
            var activeEnrolments = await Db.Enrolments.CountAsync(
                e => e.Category.EventID == id && e.Status != EnrolmentStatuses.Withdrawn, ct);
            if (capacity < activeEnrolments)
            {
                return ConflictProblem($"MaxParticipants cannot be lower than the {activeEnrolments} enrolment(s) already recorded.");
            }
        }

        ev.EventName = request.EventName.Trim();
        ev.EventDate = eventDate;
        ev.Location = request.Location.Trim();
        ev.EventType = request.EventType;
        ev.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        ev.MaxParticipants = request.MaxParticipants;
        ev.UpdatedDate = DateTime.UtcNow;

        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Delete an event that you organise.</summary>
    /// <remarks>
    /// Blocked while any enrolment is still active (Registered, Started or Completed). Withdrawn enrolments are
    /// removed with the event. Categories are deleted with the event.
    /// </remarks>
    /// <response code="204">Deleted.</response>
    /// <response code="403">Event belongs to another organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event still has active enrolments.</response>
    [HttpDelete("{id:int}")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == id, ct);
        if (ev is null)
        {
            return NotFoundProblem("Event not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || ev.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only manage events that you organise.");
        }

        var hasActiveEnrolments = await Db.Enrolments.AnyAsync(
            e => e.Category.EventID == id && e.Status != EnrolmentStatuses.Withdrawn, ct);
        if (hasActiveEnrolments)
        {
            return ConflictProblem("This event has enrolments and cannot be deleted.");
        }

        var withdrawn = await Db.Enrolments
            .Where(e => e.Category.EventID == id && e.Status == EnrolmentStatuses.Withdrawn)
            .ToListAsync(ct);
        Db.Enrolments.RemoveRange(withdrawn);
        Db.Events.Remove(ev);

        await Db.SaveChangesAsync(ct);
        return NoContent();
    }
}
