using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Attributes;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>Participants see and manage their own enrolments. Organisers see who has entered their events.</summary>
public sealed class EnrolmentsController : RaceDayController
{
    public EnrolmentsController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (!result.Ok)
        {
            Failure(result.Message);
        }

        return View(new MyEventsViewModel
        {
            Enrolments = (result.Value ?? new List<EnrolmentDto>()).OrderBy(e => e.EventDate).ToList(),
        });
    }

    /// <summary>Enrols the signed-in participant in one category of an event.</summary>
    [HttpPost]
    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId, CancellationToken ct = default)
    {
        var result = await Api.PostAsync<EnrolmentCreatedDto>("api/enrolments", new { categoryId }, ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }

        if (result.Ok)
        {
            Success("You're in! Your place is confirmed.");
        }
        else
        {
            Failure(result.Message);
        }
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    [HttpPost]
    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Withdraw(int id, CancellationToken ct = default)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{id}", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }

        if (result.Ok)
        {
            Success("You have withdrawn from the event.");
        }
        else
        {
            Failure(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Organiser view: every participant entered in one of their events.</summary>
    [RequireRole(Roles.Organiser)]
    public async Task<IActionResult> Event(int id, CancellationToken ct = default)
    {
        var (ev, early) = await LoadOwnedEventAsync(id, ct);
        if (early is not null)
        {
            return early;
        }

        var enrolments = await Api.GetAsync<List<EventEnrolmentDto>>($"api/events/{id}/enrolments", ct);
        if (HandleSessionEnded(enrolments) is { } ended)
        {
            return ended;
        }
        if (!enrolments.Ok)
        {
            Failure(enrolments.Message);
        }

        return View(new EventEnrolmentsViewModel
        {
            Event = ev!,
            Enrolments = (enrolments.Value ?? new List<EventEnrolmentDto>())
                .OrderBy(e => e.CategoryName).ThenBy(e => e.LastName).ThenBy(e => e.FirstName)
                .ToList(),
        });
    }
}
