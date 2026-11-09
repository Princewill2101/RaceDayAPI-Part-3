using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Organiser: manage the age/distance categories of an event.
[RoleAuthorize(SessionKeys.Organiser)]
public class CategoriesController : AppController
{
    public CategoriesController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        if (!await OwnsEventAsync(eventId)) return RedirectToAction("AccessDenied", "Account");

        var page = await BuildPageAsync(eventId, new CategoryFormViewModel { EventId = eventId });
        if (page == null) return RedirectToAction("Index", "ManageEvents");
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CategoryFormViewModel form)
    {
        // Cross-field rule: minimum age can't be above the maximum
        if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MinAge > form.MaxAge)
            ModelState.AddModelError("Form." + nameof(form.MaxAge), "Maximum age must be the same as or higher than minimum age.");

        if (!ModelState.IsValid)
        {
            var page = await BuildPageAsync(form.EventId, form);
            if (page == null) return RedirectToAction("Index", "ManageEvents");
            return View("Index", page);
        }

        var result = await Api.AddCategoryAsync(form.EventId, new CategoryRequestDto
        {
            CategoryName = form.CategoryName.Trim(),
            MinAge       = form.MinAge,
            MaxAge       = form.MaxAge,
            DistanceKm   = form.DistanceKm
        });
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (result.Success) Flash("Category added.");
        else Flash(result.Error, "danger");

        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        // A category that people have already entered under can't be removed
        var ev = await Api.GetEventAsync(eventId);
        var category = ev.Data?.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (category != null)
        {
            var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
            if (enrolments.Success && enrolments.Data != null &&
                enrolments.Data.Any(e => e.CategoryName == category.CategoryName))
            {
                Flash($"\"{category.CategoryName}\" has enrolments, so it can't be removed.", "warning");
                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

        var result = await Api.DeleteCategoryAsync(categoryId);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (result.Success) Flash("Category removed.");
        else Flash(result.Error, "danger");

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<CategoriesPageViewModel?> BuildPageAsync(int eventId, CategoryFormViewModel form)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null)
        {
            Flash(ev.Error, "danger");
            return null;
        }
        form.EventId = eventId;
        return new CategoriesPageViewModel { Event = ev.Data, Form = form };
    }
}
