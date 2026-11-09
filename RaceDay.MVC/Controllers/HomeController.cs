using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>Public pages: browse events, view one event, and the error page.</summary>
public sealed class HomeController : RaceDayController
{
    public HomeController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    /// <summary>Home page: upcoming events with type, time and search filters.</summary>
    /// <param name="eventType">All, Running, Walking or Cycling.</param>
    /// <param name="q">Text to match in the event name or location.</param>
    /// <param name="when">"upcoming" (default) or "all".</param>
    public async Task<IActionResult> Index(string? eventType, string? q, string? when = "upcoming", CancellationToken ct = default)
    {
        var typeFilter = EventTypes.All.FirstOrDefault(t => string.Equals(t, eventType, StringComparison.OrdinalIgnoreCase)) ?? "All";
        var upcomingOnly = !string.Equals(when, "all", StringComparison.OrdinalIgnoreCase);

        var parameters = new List<string>();
        if (typeFilter != "All")
        {
            parameters.Add($"eventType={typeFilter}");
        }
        if (upcomingOnly)
        {
            parameters.Add("upcomingOnly=true");
        }
        var path = "api/events" + (parameters.Count > 0 ? "?" + string.Join('&', parameters) : string.Empty);

        var result = await Api.GetAsync<List<EventDto>>(path, ct);
        if (!result.Ok)
        {
            ViewData["Error"] = result.Message;
        }

        var events = result.Value ?? new List<EventDto>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            events = events
                .Where(e => e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || e.Location.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new EventBrowseViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            EventType = typeFilter,
            Query = q,
            When = upcomingOnly ? "upcoming" : "all",
            TotalCategories = events.Sum(e => e.Categories.Count),
        });
    }

    /// <summary>One event: banner, details, categories, and the enrol action for participants.</summary>
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{id}", ct);
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
            return RedirectToAction(nameof(Index));
        }

        var enrolled = new HashSet<int>();
        if (Me.IsParticipant)
        {
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments", ct);
            foreach (var enrolment in (mine.Value ?? new List<EnrolmentDto>()).Where(e => e.EventId == id && e.Status != "Withdrawn"))
            {
                enrolled.Add(enrolment.CategoryId);
            }
        }

        var ev = result.Value;
        return View(new EventDetailsViewModel
        {
            Event = ev,
            IsOwner = Me.IsOrganiser && ev.OrganiserUserId == Me.UserId,
            EnrolledCategoryIds = enrolled,
            IsEventOver = ev.EventDate <= DateTime.UtcNow,
        });
    }

    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult Error() => View();
}
