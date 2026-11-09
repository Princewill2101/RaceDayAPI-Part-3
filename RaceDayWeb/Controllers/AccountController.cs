using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class AccountController : AppController
{
    public AccountController(IRaceDayApiClient api) : base(api) { }

    // ── Login ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
            return RedirectToRoleHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.LoginAsync(new LoginRequestDto
        {
            Email    = model.Email.Trim(),
            Password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        // Start a fresh session, then store who this is and which role they have
        var user = result.Data;
        string? apiCookie = HttpContext.Session.GetString(SessionKeys.ApiCookie);
        HttpContext.Session.Clear();
        if (apiCookie != null) HttpContext.Session.SetString(SessionKeys.ApiCookie, apiCookie);

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.Role);
        HttpContext.Session.SetString(SessionKeys.UserName, user.FullName);
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            HttpContext.Session.SetString(SessionKeys.ProfilePic, user.ProfilePictureUrl);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToRoleHome();
    }

    // ── Register ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
            return RedirectToRoleHome();

        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.RegisterAsync(new RegisterRequestDto
        {
            FullName = model.FullName.Trim(),
            Email    = model.Email.Trim(),
            Password = model.Password,
            Role     = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        Flash("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    // ── Logout ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Ends the API session and then this app's session
        await Api.LogoutAsync();
        HttpContext.Session.Clear();

        Flash("You have been logged out.", "info");
        return RedirectToAction(nameof(Login));
    }

    // ── Wrong role ───────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
