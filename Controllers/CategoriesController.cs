using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Models;
using RaceDay.API_Part2.Attributes;

namespace RaceDay.API_Part2.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context) => _context = context;

        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var categories = await _context.EventCategories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost("events/{eventId}/categories")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> AddCategory(int eventId, [FromBody] CreateCategoryDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound();

            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (ev.OrganiserID != organiser?.OrganiserID) return Forbid();

            var category = new EventCategory
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                Distance = dto.Distance,
                EntryFee = dto.EntryFee,
                AgeRestriction = dto.AgeRestriction
            };

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [HttpPut("categories/{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            var category = await _context.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound();

            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (category.Event.OrganiserID != organiser?.OrganiserID) return Forbid();

            category.CategoryName = dto.CategoryName;
            category.Distance = dto.Distance;
            category.EntryFee = dto.EntryFee;
            category.AgeRestriction = dto.AgeRestriction;
            category.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("categories/{id}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            var category = await _context.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound();

            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (category.Event.OrganiserID != organiser?.OrganiserID) return Forbid();

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}