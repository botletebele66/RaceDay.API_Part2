using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Models;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Attributes;


namespace RaceDay.API_Part2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EnrolmentsController(AppDbContext context) => _context = context;

        [HttpGet]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            if (participant == null) return Forbid();

            var enrolments = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Include(e => e.Result)
                .Where(e => e.ParticipantID == participant.ParticipantID)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.Status,
                    Event = new { e.Category.Event.EventID, e.Category.Event.EventName, e.Category.Event.EventDate },
                    Category = new { e.Category.CategoryID, e.Category.CategoryName, e.Category.Distance },
                    Result = e.Result == null ? null : new { e.Result.TimeCompleted, e.Result.PlacementRank }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpPost]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> Enrol([FromBody] EnrolDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            if (participant == null) return Forbid();

            var category = await _context.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID);

            if (category == null) return NotFound("Category not found.");

            var exists = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participant.ParticipantID && e.CategoryID == dto.CategoryID);

            if (exists) return BadRequest("You are already enrolled in this category.");

            var enrolment = new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                CategoryID = dto.CategoryID,
                Status = "Registered"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { enrolment.EnrolmentID, message = "Enrolled successfully." });
        }

        [HttpDelete("{id}")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            var enrolment = await _context.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null) return NotFound();
            if (enrolment.ParticipantID != participant?.ParticipantID) return Forbid();

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("events/{eventId}/enrolments")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev == null) return NotFound();

            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (ev.OrganiserID != organiser?.OrganiserID) return Forbid();

            var enrolments = await _context.Enrolments
                .Include(e => e.Category)
                .Include(e => e.Participant).ThenInclude(p => p.User)
                .Where(e => e.Category.EventID == eventId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.Status,
                    Participant = $"{e.Participant.User.FirstName} {e.Participant.User.LastName}",
                    Category = e.Category.CategoryName
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }

}