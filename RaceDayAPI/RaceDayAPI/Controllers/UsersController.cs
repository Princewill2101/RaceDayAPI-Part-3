using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/users/me ────────────────────────────────────
    /// <summary>
    /// Returns the profile of the currently logged-in user.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        var user   = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        return Ok(new UserResponse
        {
            UserId            = user.UserId,
            FullName          = user.FullName,
            Email             = user.Email,
            Role              = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    // ── PUT /api/users/me ────────────────────────────────────
    /// <summary>
    /// Update the logged-in user's own name and profile picture URL.
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest req)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        var user   = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        user.FullName          = req.FullName;
        user.ProfilePictureUrl = req.ProfilePictureUrl;
        await _db.SaveChangesAsync();

        return Ok(new UserResponse
        {
            UserId            = user.UserId,
            FullName          = user.FullName,
            Email             = user.Email,
            Role              = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }
}
