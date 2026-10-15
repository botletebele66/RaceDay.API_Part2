using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Event categories (distances or age groups). Listing is public; only the owning organiser can change them.</summary>
[ApiController]
[Route("api")]
public class CategoriesController : RaceDayControllerBase
{
    public CategoriesController(AppDbContext db) : base(db) { }

    /// <summary>List the categories of an event (public).</summary>
    /// <response code="200">Categories for the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId, CancellationToken ct)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId, ct))
        {
            return NotFoundProblem("Event not found.");
        }

        var categories = await Db.EventCategories.AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryID)
            .Select(Projections.CategoryProjection)
            .ToListAsync(ct);

        return Ok(categories);
    }

    /// <summary>Add a category to an event you organise.</summary>
    /// <remarks>Blocked for events that have already taken place. Category names must be unique within the event (case-insensitive).</remarks>
    /// <response code="201">Category added.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Event belongs to another organiser, or the caller is not an organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Duplicate name, or the event has already taken place.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCategory(int eventId, [FromBody] CategoryRequest request, CancellationToken ct)
    {
        var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId, ct);
        if (ev is null)
        {
            return NotFoundProblem("Event not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || ev.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only manage events that you organise.");
        }

        if (ev.EventDate <= DateTime.UtcNow)
        {
            return ConflictProblem("Categories cannot be added to an event that has already taken place.");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequestProblem("Category name cannot be blank.");
        }

        var name = request.CategoryName.Trim();
        var lowerName = name.ToLower();
        if (await Db.EventCategories.AnyAsync(c => c.EventID == eventId && c.CategoryName.ToLower() == lowerName, ct))
        {
            return ConflictProblem($"A category called '{name}' already exists for this event.");
        }

        var category = new EventCategory
        {
            EventID = eventId,
            CategoryName = name,
            Distance = request.Distance,
            EntryFee = request.EntryFee,
            AgeRestriction = request.AgeRestriction,
        };

        Db.EventCategories.Add(category);
        await Db.SaveChangesAsync(ct);

        return Created($"/api/events/{eventId}/categories",
            new CategoryResponse(category.CategoryID, eventId, name, category.Distance, category.EntryFee, category.AgeRestriction));
    }

    /// <summary>Update a category of an event you organise.</summary>
    /// <remarks>
    /// Changing a category does not affect enrolments already made. The age restriction is checked only when someone enrols.
    /// </remarks>
    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Category belongs to another organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Duplicate name, or the event has already taken place.</response>
    [HttpPut("categories/{id:int}")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request, CancellationToken ct)
    {
        var category = await Db.EventCategories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id, ct);
        if (category is null)
        {
            return NotFoundProblem("Category not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || category.Event.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only manage events that you organise.");
        }

        if (category.Event.EventDate <= DateTime.UtcNow)
        {
            return ConflictProblem("Categories of an event that has already taken place cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequestProblem("Category name cannot be blank.");
        }

        var name = request.CategoryName.Trim();
        var lowerName = name.ToLower();
        var eventId = category.EventID;
        if (await Db.EventCategories.AnyAsync(c => c.EventID == eventId && c.CategoryID != id && c.CategoryName.ToLower() == lowerName, ct))
        {
            return ConflictProblem($"A category called '{name}' already exists for this event.");
        }

        category.CategoryName = name;
        category.Distance = request.Distance;
        category.EntryFee = request.EntryFee;
        category.AgeRestriction = request.AgeRestriction;
        category.UpdatedDate = DateTime.UtcNow;

        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Delete a category of an event you organise.</summary>
    /// <remarks>Blocked while any active enrolment (Registered, Started or Completed) uses the category. Withdrawn enrolments are removed with it.</remarks>
    /// <response code="204">Deleted.</response>
    /// <response code="403">Category belongs to another organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Active enrolments exist, or the event has already taken place.</response>
    [HttpDelete("categories/{id:int}")]
    [AuthorizeRole(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var category = await Db.EventCategories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id, ct);
        if (category is null)
        {
            return NotFoundProblem("Category not found.");
        }

        var organiser = await GetCurrentOrganiserAsync(ct);
        if (organiser is null || category.Event.OrganiserID != organiser.OrganiserID)
        {
            return ForbiddenProblem("You can only manage events that you organise.");
        }

        if (category.Event.EventDate <= DateTime.UtcNow)
        {
            return ConflictProblem("Categories of an event that has already taken place cannot be deleted.");
        }

        var hasActiveEnrolments = await Db.Enrolments.AnyAsync(
            e => e.CategoryID == id && e.Status != EnrolmentStatuses.Withdrawn, ct);
        if (hasActiveEnrolments)
        {
            return ConflictProblem("This category has enrolments and cannot be deleted.");
        }

        var withdrawn = await Db.Enrolments
            .Where(e => e.CategoryID == id && e.Status == EnrolmentStatuses.Withdrawn)
            .ToListAsync(ct);
        Db.Enrolments.RemoveRange(withdrawn);
        Db.EventCategories.Remove(category);

        await Db.SaveChangesAsync(ct);
        return NoContent();
    }
}
