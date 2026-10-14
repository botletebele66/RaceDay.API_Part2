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
    public class EventsController : ControllerBase
    {
            private readonly AppDbContext _context;
            public EventsController(AppDbContext context) => _context = context;

            [HttpGet]
            public async Task<IActionResult> GetEvents()
            {
                var events = await _context.Events
                    .Include(e => e.Categories)
                    .Select(e => new {
                        e.EventID,
                        e.EventName,
                        e.EventDate,
                        e.Location,
                        e.EventType,
                        e.Description,
                        e.MaxParticipants,
                        Categories = e.Categories.Select(c => new { c.CategoryID, c.CategoryName, c.Distance, c.EntryFee })
                    })
                    .ToListAsync();

                return Ok(events);
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetEvent(int id)
            {
                var ev = await _context.Events
                    .Include(e => e.Categories)
                    .FirstOrDefaultAsync(e => e.EventID == id);

                if (ev == null) return NotFound();
                return Ok(ev);
            }

            [HttpPost]
            [AuthorizeRole("Organiser")]
            public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
                if (organiser == null) return Forbid();

                var ev = new Event
                {
                    OrganiserID = organiser.OrganiserID,
                    EventName = dto.EventName,
                    EventDate = dto.EventDate,
                    Location = dto.Location,
                    EventType = dto.EventType,
                    Description = dto.Description,
                    MaxParticipants = dto.MaxParticipants
                };

                _context.Events.Add(ev);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev);
            }

            [HttpPut("{id}")]
            [AuthorizeRole("Organiser")]
            public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventDto dto)
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
                if (ev == null) return NotFound();

                var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
                if (ev.OrganiserID != organiser?.OrganiserID) return Forbid();

                ev.EventName = dto.EventName;
                ev.EventDate = dto.EventDate;
                ev.Location = dto.Location;
                ev.EventType = dto.EventType;
                ev.Description = dto.Description;
                ev.MaxParticipants = dto.MaxParticipants;
                ev.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }

            [HttpDelete("{id}")]
            [AuthorizeRole("Organiser")]
            public async Task<IActionResult> DeleteEvent(int id)
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
                if (ev == null) return NotFound();

                var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
                if (ev.OrganiserID != organiser?.OrganiserID) return Forbid();

                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

    }
