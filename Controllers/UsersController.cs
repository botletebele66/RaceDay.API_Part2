using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API_Part2.Attributes;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.DTOs;
using RaceDay.API_Part2.Helper;
using RaceDay.API_Part2.Models;
using RaceDay.API_Part2.Services;

namespace RaceDay.API_Part2.Controllers;

/// <summary>User accounts and role-specific profiles.</summary>
[ApiController]
[Route("api/users")]
public class UsersController : RaceDayControllerBase
{
    private readonly IAzureBlobService _blobs;
    private readonly IConfiguration _configuration;

    public UsersController(AppDbContext db, IAzureBlobService blobs, IConfiguration configuration) : base(db)
    {
        _blobs = blobs;
        _configuration = configuration;
    }

    /// <summary>Get a user's basic details.</summary>
    /// <remarks>Any logged-in user can view basic details. The email address is returned only to the account owner.</remarks>
    /// <response code="200">User found.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [AuthorizeRole]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id, ct);
        if (user is null)
        {
            return NotFoundProblem("User not found.");
        }

        var email = IsCurrentUser(id) ? user.Email : null;
        return Ok(new UserResponse(user.UserID, user.FirstName, user.LastName, user.UserRole, email));
    }

    /// <summary>Update your own account and profile.</summary>
    /// <remarks>
    /// Only the account owner can call this. Account fields (name, email, optional password) apply to every role.
    /// Participants update DateOfBirth and EmergencyContact. Organisers update CompanyName and RegistrationNumber,
    /// which are required. Fields for the other role are ignored.
    /// </remarks>
    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Attempting to update another user's account.</response>
    /// <response code="409">Email or registration number already belongs to another account.</response>
    [HttpPut("{id:int}")]
    [AuthorizeRole]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (!IsCurrentUser(id))
        {
            return ForbiddenProblem("You can only update your own account.");
        }

        var user = await Db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserID == id, ct);
        if (user is null)
        {
            return NotFoundProblem("User not found.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequestProblem("First name and last name cannot be blank.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email && u.UserID != id, ct))
        {
            return ConflictProblem("That email address already belongs to another account.");
        }

        if (user.UserRole == Roles.Organiser)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.RegistrationNumber))
            {
                return BadRequestProblem("CompanyName and RegistrationNumber are required for Organisers.");
            }

            var registrationNumber = request.RegistrationNumber.Trim();
            if (await Db.Organisers.AnyAsync(o => o.RegistrationNumber == registrationNumber && o.UserID != id, ct))
            {
                return ConflictProblem("An organiser with this registration number already exists.");
            }

            if (user.Organiser is null)
            {
                return ConflictProblem("This organiser profile is incomplete. Contact the system administrator.");
            }

            user.Organiser.CompanyName = request.CompanyName.Trim();
            user.Organiser.RegistrationNumber = registrationNumber;
            user.Organiser.PhoneNumber = request.PhoneNumber?.Trim();
            user.Organiser.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            if (request.DateOfBirth is { } dob && dob.Date > DateTime.UtcNow.Date)
            {
                return BadRequestProblem("Date of birth cannot be in the future.");
            }

            if (user.Participant is null)
            {
                return ConflictProblem("This participant profile is incomplete. Contact the system administrator.");
            }

            user.Participant.DateOfBirth = request.DateOfBirth?.Date;
            user.Participant.PhoneNumber = request.PhoneNumber?.Trim();
            user.Participant.EmergencyContact = request.EmergencyContact?.Trim();
            user.Participant.UpdatedDate = DateTime.UtcNow;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.UpdatedDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.Password = PasswordHelper.HashPassword(request.Password);
        }

        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Get your role-specific profile.</summary>
    /// <remarks>Only the account owner can view the profile. Participants see date of birth and emergency contact. Organisers see company and registration details.</remarks>
    /// <response code="200">Profile returned.</response>
    /// <response code="403">Attempting to view another user's profile.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}/profile")]
    [AuthorizeRole]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProfile(int id, CancellationToken ct)
    {
        if (!IsCurrentUser(id))
        {
            return ForbiddenProblem("You can only view your own profile.");
        }

        var user = await Db.Users
            .AsNoTracking()
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserID == id, ct);
        if (user is null)
        {
            return NotFoundProblem("User not found.");
        }

        var profile = user.UserRole == Roles.Organiser
            ? new UserProfileResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.UserRole,
                user.Organiser?.PhoneNumber, null, null, user.Organiser?.CompanyName, user.Organiser?.RegistrationNumber,
                user.ProfileImageUrl)
            : new UserProfileResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.UserRole,
                user.Participant?.PhoneNumber, user.Participant?.DateOfBirth, user.Participant?.EmergencyContact, null, null,
                user.ProfileImageUrl);

        return Ok(profile);
    }

    /// <summary>Upload or replace your profile picture.</summary>
    /// <remarks>
    /// Accepts a JPG, PNG, WEBP or GIF of up to 5 MB, sent as multipart form data in a field called <c>file</c>.
    /// The picture is stored through the API's blob service; the previous picture is removed.
    /// </remarks>
    /// <response code="200">Picture stored. Returns its URL.</response>
    /// <response code="400">No file, or the file is not an accepted image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Attempting to change another user's picture.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("{id:int}/picture")]
    [AuthorizeRole]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadProfilePicture(int id, IFormFile? file, CancellationToken ct)
    {
        if (!IsCurrentUser(id))
        {
            return ForbiddenProblem("You can only change your own profile picture.");
        }

        if (file is null || file.Length == 0)
        {
            return BadRequestProblem("Choose an image to upload.");
        }

        var user = await Db.Users.FirstOrDefaultAsync(u => u.UserID == id, ct);
        if (user is null)
        {
            return NotFoundProblem("User not found.");
        }

        string storedUrl;
        try
        {
            await using var stream = file.OpenReadStream();
            storedUrl = await _blobs.UploadImageAsync(stream, file.FileName, file.ContentType, "profiles", ct);
        }
        catch (InvalidImageException ex)
        {
            return BadRequestProblem(ex.Message);
        }

        var previousUrl = user.ProfileImageUrl;
        user.ProfileImageUrl = PublicUrls.ToPublic(storedUrl, _configuration, Request);
        user.UpdatedDate = DateTime.UtcNow;
        await Db.SaveChangesAsync(ct);

        await _blobs.DeleteAsync(previousUrl, ct);

        return Ok(new { profileImageUrl = user.ProfileImageUrl });
    }
}
