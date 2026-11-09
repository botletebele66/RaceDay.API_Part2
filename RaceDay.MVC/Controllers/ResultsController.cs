using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Attributes;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>Participants see their race history. Organisers enter finish times and positions after an event.</summary>
public sealed class ResultsController : RaceDayController
{
    public ResultsController(ApiClient api, CurrentUser me) : base(api, me)
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

        // Only enrolments with a recorded result appear in the race history.
        var raced = (result.Value ?? new List<EnrolmentDto>())
            .Where(e => e.TimeCompleted is not null || e.PlacementRank is not null)
            .OrderByDescending(e => e.EventDate)
            .ToList();

        return View(new MyResultsViewModel { Results = raced });
    }

    /// <summary>Results grid for one of the organiser's events.</summary>
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

        var rows = new List<ResultRowViewModel>();
        foreach (var enrolment in (enrolments.Value ?? new List<EventEnrolmentDto>())
                     .Where(e => e.Status != "Withdrawn")
                     .OrderBy(e => e.CategoryName).ThenBy(e => e.LastName))
        {
            var existing = await Api.GetAsync<ResultDto>($"api/results/{enrolment.EnrolmentId}", ct);
            rows.Add(new ResultRowViewModel
            {
                EnrolmentId = enrolment.EnrolmentId,
                ParticipantName = $"{enrolment.FirstName} {enrolment.LastName}",
                Email = enrolment.Email,
                CategoryName = enrolment.CategoryName,
                Status = enrolment.Status,
                Existing = existing.Ok ? existing.Value : null,
            });
        }

        return View(new ResultsEntryViewModel { Event = ev!, Rows = rows });
    }

    /// <summary>Saves the times and positions that were typed in. Blank rows are skipped.</summary>
    [HttpPost]
    [RequireRole(Roles.Organiser)]
    public async Task<IActionResult> Record(RecordResultsInput input, CancellationToken ct = default)
    {
        var (_, early) = await LoadOwnedEventAsync(input.EventId, ct);
        if (early is not null)
        {
            return early;
        }

        var saved = 0;
        var problems = new List<string>();

        foreach (var row in input.Rows)
        {
            if (row.Hours is null && row.Minutes is null && row.Seconds is null && row.Position is null)
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(row.Name) ? "a participant" : row.Name;
            var hours = row.Hours ?? 0;
            var minutes = row.Minutes ?? 0;
            var seconds = row.Seconds ?? 0;

            if (row.Position is null or < 1)
            {
                problems.Add($"Enter a finishing position for {name}.");
                continue;
            }
            if (hours is < 0 or > 99 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
            {
                problems.Add($"Check the finish time for {name}: minutes and seconds go up to 59.");
                continue;
            }

            var totalSeconds = hours * 3600 + minutes * 60 + seconds;
            if (totalSeconds <= 0)
            {
                problems.Add($"Enter a finish time for {name}.");
                continue;
            }

            var result = await Api.PostAsync<ResultDto>("api/results", new
            {
                enrolmentId = row.EnrolmentId,
                timeCompletedSeconds = totalSeconds,
                placementRank = row.Position.Value,
            }, ct);

            if (HandleSessionEnded(result) is { } ended)
            {
                return ended;
            }
            if (result.Ok)
            {
                saved++;
            }
            else
            {
                problems.Add($"{name}: {result.Message}");
            }
        }

        if (saved > 0)
        {
            Success(saved == 1 ? "1 result saved and published to the participant." : $"{saved} results saved and published to participants.");
        }
        if (problems.Count > 0)
        {
            Failure(string.Join(" ", problems.Distinct()));
        }
        if (saved == 0 && problems.Count == 0)
        {
            Failure("Nothing to save. Enter a finish time and position first.");
        }

        return RedirectToAction(nameof(Event), new { id = input.EventId });
    }
}
