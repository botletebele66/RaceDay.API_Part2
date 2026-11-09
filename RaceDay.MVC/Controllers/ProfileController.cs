using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Attributes;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>The signed-in user's details, password and profile picture.</summary>
[RequireRole]
public sealed class ProfileController : RaceDayController
{
    public ProfileController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await Api.GetAsync<UserProfileDto>($"api/users/{Me.UserId}/profile", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (NotFoundIfMissing(result) is { } notFound)
        {
            return notFound;
        }
        if (!result.Ok || result.Value is null)
        {
            Failure(result.Message);
            return RedirectToAction("Index", "Dashboard");
        }

        var p = result.Value;
        return View(new ProfileFormViewModel
        {
            UserId = p.UserId,
            Role = p.Role,
            ProfileImageUrl = p.ProfileImageUrl,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            EmergencyContact = p.EmergencyContact,
            CompanyName = p.CompanyName,
            RegistrationNumber = p.RegistrationNumber,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileFormViewModel model, CancellationToken ct = default)
    {
        // The user can only ever update their own account.
        model.UserId = Me.UserId ?? 0;
        model.Role = Me.Role ?? model.Role;

        if (model.IsOrganiser)
        {
            if (string.IsNullOrWhiteSpace(model.CompanyName))
            {
                ModelState.AddModelError(nameof(model.CompanyName), "Enter your company or club name.");
            }
            if (string.IsNullOrWhiteSpace(model.RegistrationNumber))
            {
                ModelState.AddModelError(nameof(model.RegistrationNumber), "Enter your registration number.");
            }
        }
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var hasNewPassword = !string.IsNullOrWhiteSpace(model.NewPassword);
        var result = await Api.PutAsync<object>($"api/users/{model.UserId}", new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            email = model.Email.Trim(),
            password = hasNewPassword ? model.NewPassword : null,
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            dateOfBirth = model.IsOrganiser ? null : model.DateOfBirth,
            emergencyContact = model.IsOrganiser ? null : model.EmergencyContact?.Trim(),
            companyName = model.IsOrganiser ? model.CompanyName?.Trim() : null,
            registrationNumber = model.IsOrganiser ? model.RegistrationNumber?.Trim() : null,
        }, ct);

        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Index", model);
        }

        Me.UpdateName(model.FirstName.Trim(), model.LastName.Trim(), model.Email.Trim());
        Success(hasNewPassword ? "Profile and password updated." : "Profile updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Picture(IFormFile? picture, CancellationToken ct = default)
    {
        if (picture is null || picture.Length == 0)
        {
            Failure("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        if (ImageUpload.Validate(picture) is { } problem)
        {
            Failure(problem);
            return RedirectToAction(nameof(Index));
        }

        await using var stream = picture.OpenReadStream();
        using var content = ImageUpload.ToContent(picture, stream);
        var result = await Api.PostMultipartAsync<object>($"api/users/{Me.UserId}/picture", content, ct);

        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }

        if (result.Ok)
        {
            Success("Profile picture updated.");
        }
        else
        {
            Failure(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
