using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Controllers;

/// <summary>Registration, login, logout and session renewal. Authentication is server-side session based.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext db, IConfiguration configuration) : base(db)
    {
        _configuration = configuration;
    }

    /// <summary>Register a new Participant or Organiser account.</summary>
    /// <remarks>
    /// Public endpoint. Participants can self-register. Organiser registration needs the organiser invite code
    /// set by the system administrator (Registration:OrganiserInviteCode). If no code is configured, Organiser
    /// registration is closed. Organisers must also supply CompanyName and RegistrationNumber.
    /// Passwords are stored as BCrypt hashes.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user (log in next).</response>
    /// <response code="400">Validation failed (for example a short password, invalid role, or missing organiser fields).</response>
    /// <response code="403">Organiser invite code missing or incorrect, or Organiser registration is closed.</response>
    /// <response code="409">Email address or registration number already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequestProblem("First name and last name cannot be blank.");
        }

        if (request.DateOfBirth is { } dob && dob.Date > DateTime.UtcNow.Date)
        {
            return BadRequestProblem("Date of birth cannot be in the future.");
        }

        if (request.Role == Roles.Organiser)
        {
            var configuredCode = _configuration["Registration:OrganiserInviteCode"];
            if (string.IsNullOrWhiteSpace(configuredCode))
            {
                return ForbiddenProblem("Organiser registration is currently closed.");
            }

            if (!string.Equals(request.OrganiserInviteCode, configuredCode, StringComparison.Ordinal))
            {
                return ForbiddenProblem("A valid organiser invite code is required to register as an Organiser.");
            }

            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.RegistrationNumber))
            {
                return BadRequestProblem("CompanyName and RegistrationNumber are required for Organisers.");
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return ConflictProblem("An account with this email address already exists.");
        }

        var registrationNumber = request.RegistrationNumber?.Trim();
        if (request.Role == Roles.Organiser && await Db.Organisers.AnyAsync(o => o.RegistrationNumber == registrationNumber, ct))
        {
            return ConflictProblem("An organiser with this registration number already exists.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Password = PasswordHelper.HashPassword(request.Password),
            UserRole = request.Role,
        };

        if (request.Role == Roles.Organiser)
        {
            user.Organiser = new Organiser
            {
                CompanyName = request.CompanyName!.Trim(),
                RegistrationNumber = registrationNumber,
                PhoneNumber = request.PhoneNumber?.Trim(),
            };
        }
        else
        {
            user.Participant = new Participant
            {
                DateOfBirth = request.DateOfBirth?.Date,
                PhoneNumber = request.PhoneNumber?.Trim(),
                EmergencyContact = request.EmergencyContact?.Trim(),
            };
        }

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        return Created($"/api/users/{user.UserID}", ToResponse(user));
    }

    /// <summary>Log in and start a server-side session that stores the user ID and role.</summary>
    /// <remarks>
    /// On success the response sets the session cookie (.RaceDay.Session). Swagger UI and the browser send it
    /// automatically on later calls. Any previous session data is discarded first.
    /// </remarks>
    /// <response code="200">Logged in. Returns the user.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !PasswordHelper.VerifyPassword(request.Password, user.Password))
        {
            return UnauthorizedProblem("Invalid email or password.");
        }

        await HttpContext.Session.LoadAsync(ct);
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.UserRole);

        return Ok(ToResponse(user));
    }

    /// <summary>End the current session and clear the session cookie.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [AuthorizeRole]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await HttpContext.Session.LoadAsync(ct);
        HttpContext.Session.Clear();
        Response.Cookies.Delete(SessionKeys.CookieName);
        return NoContent();
    }

    /// <summary>Renew the current session and return the logged-in user.</summary>
    /// <remarks>Equivalent of issuing a fresh token: resets the 30-minute idle timeout. Use it to keep a session alive.</remarks>
    /// <response code="200">Session renewed.</response>
    /// <response code="401">Not logged in, or the account no longer exists (the session is then cleared).</response>
    [HttpGet("refresh-token")]
    [AuthorizeRole]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var userId = CurrentUserId!.Value;
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId, ct);

        if (user is null)
        {
            HttpContext.Session.Clear();
            return UnauthorizedProblem("The account for this session no longer exists.");
        }

        // Writing the values again marks the session as modified, which renews the idle timeout and cookie.
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.UserRole);

        return Ok(new SessionResponse(ToResponse(user), (int)SessionKeys.IdleTimeout.TotalMinutes));
    }

    private static UserResponse ToResponse(User user) =>
        new(user.UserID, user.FirstName, user.LastName, user.UserRole, user.Email);
}
