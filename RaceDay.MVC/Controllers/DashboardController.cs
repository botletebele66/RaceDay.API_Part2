using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Attributes;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>The signed-in home page. Organisers and participants see different summaries.</summary>
[RequireRole]
public sealed class DashboardController : RaceDayController
{
    public DashboardController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    public async Task<IActionResult> Index(CancellationToken ct = default) =>
        Me.IsOrganiser ? await OrganiserAsync(ct) : await ParticipantAsync(ct);

    private async Task<IActionResult> OrganiserAsync(CancellationToken ct)
    {
        var (rows, early) = await LoadOrganiserRowsAsync(ct);
        if (early is not null)
        {
            return early;
        }

        var now = DateTime.UtcNow;
        return View(new DashboardViewModel
        {
            EventCount = rows.Count,
            UpcomingEventCount = rows.Count(r => r.Event.EventDate > now),
            TotalEnrolments = rows.Sum(r => r.EnrolmentCount),
            EventRows = rows,
        });
    }

    private async Task<IActionResult> ParticipantAsync(CancellationToken ct)
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

        var enrolments = result.Value ?? new List<EnrolmentDto>();
        var now = DateTime.UtcNow;
        var upcoming = enrolments
            .Where(e => e.Status == "Registered" && e.EventDate > now)
            .OrderBy(e => e.EventDate)
            .ToList();

        return View(new DashboardViewModel
        {
            UpcomingEnrolmentCount = upcoming.Count,
            CompletedCount = enrolments.Count(e => e.Status == "Completed" || e.PlacementRank is not null),
            UpcomingEnrolments = upcoming.Take(5).ToList(),
        });
    }
}
