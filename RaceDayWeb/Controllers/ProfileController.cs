using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Filters;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

// Any logged-in user can view and update their own profile.
[RoleAuthorize]
public class ProfileController : AppController
{
    public ProfileController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync();
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            Flash(result.Error, "danger");
            return RedirectToRoleHome();
        }

        return View(ToViewModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        // Only the name is edited here. The other fields aren't posted by this form
        // (the picture has its own form), so leave them out of validation.
        ModelState.Remove(nameof(ProfileViewModel.Picture));
        ModelState.Remove(nameof(ProfileViewModel.Email));
        ModelState.Remove(nameof(ProfileViewModel.Role));
        ModelState.Remove(nameof(ProfileViewModel.ProfilePictureUrl));

        var current = await Api.GetProfileAsync();
        var expired = HandleExpiredSession(current);
        if (expired != null) return expired;
        if (!current.Success || current.Data == null)
        {
            Flash(current.Error, "danger");
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            // Redisplay with the fields we don't edit filled back in
            model.Email = current.Data.Email;
            model.Role = current.Data.Role;
            model.ProfilePictureUrl = current.Data.ProfilePictureUrl;
            return View("Index", model);
        }

        // The API replaces the picture URL on every update, so send the existing one back
        var result = await Api.UpdateProfileAsync(new UpdateProfileRequestDto
        {
            FullName          = model.FullName.Trim(),
            ProfilePictureUrl = current.Data.ProfilePictureUrl
        });

        if (!result.Success || result.Data == null)
        {
            Flash(result.Error, "danger");
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString(SessionKeys.UserName, result.Data.FullName);
        Flash("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Flash("Please choose an image to upload.", "warning");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture);
        var expired = HandleExpiredSession(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            Flash(result.Error, "danger");
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrEmpty(result.Data.ProfilePictureUrl))
            HttpContext.Session.SetString(SessionKeys.ProfilePic, result.Data.ProfilePictureUrl);

        Flash("Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(UserDto u) => new()
    {
        FullName          = u.FullName,
        Email             = u.Email,
        Role              = u.Role,
        ProfilePictureUrl = u.ProfilePictureUrl
    };
}
