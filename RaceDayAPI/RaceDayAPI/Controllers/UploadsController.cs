using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

// Image uploads (Part 3). Files go to Azure Blob Storage; only the URL is saved in SQL.
[ApiController]
public class UploadsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UploadsController(AppDbContext db, IBlobStorageService blobs)
    {
        _db    = db;
        _blobs = blobs;
    }

    // ── POST /api/events/{id}/banner ─────────────────────────
    /// <summary>Upload a banner image for an event. Owning Organiser only.</summary>
    [HttpPost("api/events/{id}/banner")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (ev.OrganiserId != userId) return Forbid();

        try
        {
            ev.BannerImageUrl = await _blobs.UploadImageAsync(file, "banners");
            await _db.SaveChangesAsync();
            return Ok(new { bannerImageUrl = ev.BannerImageUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(503, new { message = "Image storage is not available right now." });
        }
    }

    // ── POST /api/users/me/picture ───────────────────────────
    /// <summary>Upload a profile picture for the logged-in user.</summary>
    [HttpPost("api/users/me/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You must be logged in." });

        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        try
        {
            user.ProfilePictureUrl = await _blobs.UploadImageAsync(file, "profiles");
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(503, new { message = "Image storage is not available right now." });
        }
    }
}
