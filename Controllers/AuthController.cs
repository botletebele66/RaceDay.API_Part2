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
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Email already in use.");
            }

            if (dto.Role != "Organiser" && dto.Role != "Participant")
            {
                return BadRequest("Invalid role.");
            }

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = PasswordHelper.HashPassword(dto.Password),
                UserRole = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (dto.Role == "Organiser")
            {
                if (string.IsNullOrWhiteSpace(dto.CompanyName))
                    return BadRequest("CompanyName is required for Organiser.");

                _context.Organisers.Add(new Organiser
                {
                    UserID = user.UserID,
                    CompanyName = dto.CompanyName,
                    RegistrationNumber = dto.RegistrationNumber,
                    PhoneNumber = dto.PhoneNumber
                });
            }
            else // Participant
            {
                _context.Participants.Add(new Participant
                {
                    UserID = user.UserID,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    EmergencyContact = dto.EmergencyContact
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful. Please log in." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.Password))
            {
                return Unauthorized("Invalid credentials.");
            }

            // Set session data
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserRole", user.UserRole);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return Ok(new
            {
                user.UserID,
                user.FirstName,
                user.LastName,
                user.UserRole
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            // In a real scenario, you'd fetch the full user from DB here.
            return Ok(new
            {
                UserID = userId,
                Role = HttpContext.Session.GetString("UserRole"),
                Email = HttpContext.Session.GetString("UserEmail")
            });
        }
    }
}
