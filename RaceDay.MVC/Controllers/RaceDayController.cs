using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>
/// Shared helpers for the RaceDay controllers: flash messages, session expiry, ownership checks and API failures.
/// Controllers hold no business rules. The API decides what is allowed; these helpers only turn its answers into pages.
/// </summary>
public abstract class RaceDayController : Controller
{
    protected RaceDayController(ApiClient api, CurrentUser me)
    {
        Api = api;
        Me = me;
    }

    protected ApiClient Api { get; }

    protected CurrentUser Me { get; }

    /// <summary>Shows a green message on the next page.</summary>
    protected void Success(string message) => TempData["Success"] = message;

    /// <summary>Shows a red message on the next page.</summary>
    protected void Failure(string message) => TempData["Error"] = message;

    /// <summary>The first validation message on the form, for a short error on redirects.</summary>
    protected string? FirstModelError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

    /// <summary>403 page for signed-in users who may not see this.</summary>
    protected IActionResult AccessDenied() =>
        new ViewResult { ViewName = "~/Views/Shared/AccessDenied.cshtml", StatusCode = StatusCodes.Status403Forbidden };

    /// <summary>
    /// When the API reports that the session has ended, signs the user out and sends them to sign in.
    /// Returns null for every other result.
    /// </summary>
    protected IActionResult? HandleSessionEnded(ApiResult result)
    {
        if (!result.IsUnauthorized)
        {
            return null;
        }

        Me.SignOut();
        TempData["Error"] = "Your session has ended. Please sign in again.";
        var returnUrl = Request.Method == HttpMethods.Get ? Request.Path + Request.QueryString : null;
        return RedirectToAction("Login", "Auth", new { returnUrl });
    }

    /// <summary>A missing record on a GET page becomes a real 404.</summary>
    protected IActionResult? NotFoundIfMissing(ApiResult result) =>
        result.Status == HttpStatusCode.NotFound ? NotFound() : null;

    /// <summary>
    /// Loads an event that the signed-in organiser owns. When it cannot, <c>Early</c> holds the response to return
    /// (sign-in, 404, 403 or a redirect with a message).
    /// </summary>
    protected async Task<(EventDto? Event, IActionResult? Early)> LoadOwnedEventAsync(int eventId, CancellationToken ct)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{eventId}", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return (null, ended);
        }
        if (NotFoundIfMissing(result) is { } notFound)
        {
            return (null, notFound);
        }
        if (!result.Ok || result.Value is null)
        {
            Failure(result.Message);
            return (null, RedirectToAction("Index", "Events"));
        }
        if (result.Value.OrganiserUserId != Me.UserId)
        {
            return (null, AccessDenied());
        }
        return (result.Value, null);
    }

    /// <summary>
    /// The organiser's own events, in date order, each with its count of active (not withdrawn) enrolments.
    /// Counting is presentation only; the API is the source of the figures.
    /// </summary>
    protected async Task<(List<OrganiserEventRow> Rows, IActionResult? Early)> LoadOrganiserRowsAsync(CancellationToken ct)
    {
        var events = await Api.GetAsync<List<EventDto>>("api/events", ct);
        if (HandleSessionEnded(events) is { } ended)
        {
            return (new List<OrganiserEventRow>(), ended);
        }
        if (!events.Ok)
        {
            Failure(events.Message);
        }

        var mine = (events.Value ?? new List<EventDto>())
            .Where(e => e.OrganiserUserId == Me.UserId)
            .OrderBy(e => e.EventDate)
            .ToList();

        var rows = new List<OrganiserEventRow>();
        foreach (var ev in mine)
        {
            var enrolments = await Api.GetAsync<List<EventEnrolmentDto>>($"api/events/{ev.EventId}/enrolments", ct);
            var active = (enrolments.Value ?? new List<EventEnrolmentDto>()).Count(e => e.Status != "Withdrawn");
            rows.Add(new OrganiserEventRow(ev, active));
        }
        return (rows, null);
    }
}
