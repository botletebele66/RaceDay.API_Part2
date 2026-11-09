using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Attributes;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;

namespace RaceDay.MVC.Controllers;

/// <summary>Organiser pages: create, edit, delete and manage events and their categories.</summary>
[RequireRole(Roles.Organiser)]
public sealed class EventsController : RaceDayController
{
    public EventsController(ApiClient api, CurrentUser me) : base(api, me)
    {
    }

    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var (rows, early) = await LoadOrganiserRowsAsync(ct);
        if (early is not null)
        {
            return early;
        }
        return View(new OrganiserEventsViewModel { Rows = rows });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel
        {
            // A sensible default: 08:00 in four weeks' time.
            EventDate = SouthAfricaTime.Now.Date.AddDays(28).AddHours(8),
            Categories = [new CategoryInput { CategoryName = "10km", Distance = 10, EntryFee = 100 }],
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken ct = default)
    {
        ValidateEventForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PostAsync<EventDto>("api/events", new
        {
            eventName = model.EventName.Trim(),
            eventDate = SouthAfricaTime.ToApi(model.EventDate!.Value),
            location = model.Location.Trim(),
            eventType = model.EventType,
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            maxParticipants = model.MaxParticipants,
            categories = model.Categories.Select(c => new
            {
                categoryName = c.CategoryName.Trim(),
                distance = c.Distance,
                entryFee = c.EntryFee,
                ageRestriction = c.AgeRestriction,
            }),
        }, ct);

        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (!result.Ok || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var bannerProblem = await UploadBannerAsync(result.Value.EventId, model.Banner, ct);
        if (bannerProblem is null)
        {
            Success("Event created. Share it with your participants.");
        }
        else
        {
            Failure($"The event was created, but the banner was not uploaded. {bannerProblem}");
        }
        return RedirectToAction(nameof(Manage), new { id = result.Value.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct = default)
    {
        var (ev, early) = await LoadOwnedEventAsync(id, ct);
        if (early is not null)
        {
            return early;
        }

        return View(new EventFormViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            EventDate = SouthAfricaTime.FromApi(ev.EventDate),
            Location = ev.Location,
            EventType = ev.EventType,
            Description = ev.Description,
            MaxParticipants = ev.MaxParticipants,
            CurrentImageUrl = ev.ImageUrl,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken ct = default)
    {
        var (ev, early) = await LoadOwnedEventAsync(id, ct);
        if (early is not null)
        {
            return early;
        }

        model.EventId = id;
        ValidateEventForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PutAsync<object>($"api/events/{id}", new
        {
            eventName = model.EventName.Trim(),
            eventDate = SouthAfricaTime.ToApi(model.EventDate!.Value),
            location = model.Location.Trim(),
            eventType = model.EventType,
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            maxParticipants = model.MaxParticipants,
        }, ct);

        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.CurrentImageUrl = ev!.ImageUrl;
            return View(model);
        }

        var bannerProblem = await UploadBannerAsync(id, model.Banner, ct);
        if (bannerProblem is null)
        {
            Success("Event updated.");
        }
        else
        {
            Failure($"The event was saved, but the banner was not uploaded. {bannerProblem}");
        }
        return RedirectToAction(nameof(Manage), new { id });
    }

    public async Task<IActionResult> Manage(int id, CancellationToken ct = default)
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

        return View(new EventManageViewModel
        {
            Event = ev!,
            Enrolments = enrolments.Value ?? new List<EventEnrolmentDto>(),
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, CategoryInput category, CancellationToken ct = default)
    {
        var (_, early) = await LoadOwnedEventAsync(eventId, ct);
        if (early is not null)
        {
            return early;
        }

        if (!ModelState.IsValid)
        {
            Failure(FirstModelError() ?? "Check the category details and try again.");
            return RedirectToAction(nameof(Manage), new { id = eventId });
        }

        var result = await Api.PostAsync<object>($"api/events/{eventId}/categories", new
        {
            categoryName = category.CategoryName.Trim(),
            distance = category.Distance,
            entryFee = category.EntryFee,
            ageRestriction = category.AgeRestriction,
        }, ct);

        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (result.Ok)
        {
            Success($"Added the {category.CategoryName.Trim()} category.");
        }
        else
        {
            Failure(result.Message);
        }
        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId, CancellationToken ct = default)
    {
        var (_, early) = await LoadOwnedEventAsync(eventId, ct);
        if (early is not null)
        {
            return early;
        }

        var result = await Api.DeleteAsync($"api/categories/{categoryId}", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (result.Ok)
        {
            Success("Category removed.");
        }
        else
        {
            Failure(result.Message);
        }
        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var (_, early) = await LoadOwnedEventAsync(id, ct);
        if (early is not null)
        {
            return early;
        }

        var result = await Api.DeleteAsync($"api/events/{id}", ct);
        if (HandleSessionEnded(result) is { } ended)
        {
            return ended;
        }
        if (result.Ok)
        {
            Success("Event deleted.");
        }
        else
        {
            // For example, an event that already has enrolments cannot be deleted.
            Failure(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Checks dates, event type, banner and category names before the API is called.</summary>
    private void ValidateEventForm(EventFormViewModel model)
    {
        if (model.EventDate is { } date && date <= SouthAfricaTime.Now)
        {
            ModelState.AddModelError(nameof(model.EventDate), "The start date must be in the future.");
        }

        if (!EventTypes.All.Contains(model.EventType))
        {
            ModelState.AddModelError(nameof(model.EventType), "Choose Running, Walking or Cycling.");
        }

        if (model.Banner is { Length: > 0 } banner && ImageUpload.Validate(banner) is { } bannerError)
        {
            ModelState.AddModelError(nameof(model.Banner), bannerError);
        }

        var duplicate = model.Categories
            .Where(c => !string.IsNullOrWhiteSpace(c.CategoryName))
            .GroupBy(c => c.CategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            ModelState.AddModelError(string.Empty, $"The category name '{duplicate.Key}' is used more than once.");
        }
    }

    /// <summary>Uploads the banner through the API. Returns a problem message, or null when there is nothing wrong.</summary>
    private async Task<string?> UploadBannerAsync(int eventId, IFormFile? banner, CancellationToken ct)
    {
        if (banner is null || banner.Length == 0)
        {
            return null;
        }

        if (ImageUpload.Validate(banner) is { } problem)
        {
            return problem;
        }

        await using var stream = banner.OpenReadStream();
        using var content = ImageUpload.ToContent(banner, stream);
        var result = await Api.PostMultipartAsync<object>($"api/events/{eventId}/image", content, ct);
        return result.Ok ? null : result.Message;
    }
}
