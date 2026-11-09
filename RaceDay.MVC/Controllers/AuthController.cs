using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>Sign in, registration and sign out. Credentials go to the API, which starts the session.</summary>
public sealed class AuthController : RaceDayController
{
    public AuthController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Me.IsAuthenticated)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PostAsync<UserDto>("api/auth/login", new { email = model.Email.Trim(), password = model.Password }, ct);
        if (!result.Ok || result.Value is null)
        {
            ModelState.AddModelError(string.Empty,
                result.Status == HttpStatusCode.Unauthorized ? "That email and password don't match. Try again." : result.Message);
            return View(model);
        }

        // The API's session cookie is stored by the ApiClient. Without it nothing else would work.
        if (string.IsNullOrEmpty(HttpContext.Session.GetString(MvcSessionKeys.ApiCookie)))
        {
            ModelState.AddModelError(string.Empty, "Sign-in worked, but no session was started. Please try again.");
            return View(model);
        }

        Me.SignIn(result.Value);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (Me.IsAuthenticated)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct = default)
    {
        var isOrganiser = model.Role == Roles.Organiser;
        if (isOrganiser)
        {
            if (string.IsNullOrWhiteSpace(model.CompanyName))
            {
                ModelState.AddModelError(nameof(model.CompanyName), "Enter your company or club name.");
            }
            if (string.IsNullOrWhiteSpace(model.RegistrationNumber))
            {
                ModelState.AddModelError(nameof(model.RegistrationNumber), "Enter your registration number.");
            }
            if (string.IsNullOrWhiteSpace(model.OrganiserInviteCode))
            {
                ModelState.AddModelError(nameof(model.OrganiserInviteCode), "Enter the organiser invite code.");
            }
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PostAsync<UserDto>("api/auth/register", new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role,
            organiserInviteCode = isOrganiser ? model.OrganiserInviteCode?.Trim() : null,
            companyName = isOrganiser ? model.CompanyName?.Trim() : null,
            registrationNumber = isOrganiser ? model.RegistrationNumber?.Trim() : null,
            dateOfBirth = isOrganiser ? null : model.DateOfBirth,
            phoneNumber = model.PhoneNumber?.Trim(),
            emergencyContact = isOrganiser ? null : model.EmergencyContact?.Trim(),
        }, ct);

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty,
                result.Status == HttpStatusCode.Conflict
                    ? "An account with that email address already exists. Try signing in instead."
                    : result.Message);
            return View(model);
        }

        Success("Your account is ready. Sign in to continue.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        // Tell the API to end its session too. A failure here still signs the user out of the front end.
        await Api.PostAsync<object>("api/auth/logout", null, ct);
        Me.SignOut();
        Success("You have signed out.");
        return RedirectToAction("Index", "Home");
    }
}
