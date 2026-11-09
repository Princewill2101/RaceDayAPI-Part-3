using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    // ── POST /api/auth/register ──────────────────────────────
    /// <summary>
    /// Create a new account. Role must be "Organiser" or "Participant".
    /// The password is hashed with BCrypt before it is stored.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // Validate role value
        if (req.Role != "Organiser" && req.Role != "Participant")
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        // Check email is not already taken
        bool emailTaken = await _db.Users.AnyAsync(u => u.Email == req.Email);
        if (emailTaken)
            return Conflict(new { message = "An account with this email already exists." });

        // Hash the password - never store it as plain text
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var user = new User
        {
            FullName     = req.FullName,
            Email        = req.Email,
            PasswordHash = passwordHash,
            Role         = req.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { userId = user.UserId, role = user.Role,
            message = "Account created successfully." });
    }

    // ── POST /api/auth/login ─────────────────────────────────
    /// <summary>
    /// Log in with email and password. Stores UserId and Role in the session.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        // Return 401 for both "no user" and "wrong password" - don't reveal which
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        // Store the user's id and role in the session
        SessionHelper.SetUser(HttpContext.Session, user.UserId, user.Role);

        return Ok(new UserResponse
        {
            UserId           = user.UserId,
            FullName         = user.FullName,
            Email            = user.Email,
            Role             = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    // ── POST /api/auth/logout ────────────────────────────────
    /// <summary>
    /// End the current user's session.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return Unauthorized(new { message = "You are not logged in." });

        SessionHelper.ClearUser(HttpContext.Session);
        return Ok(new { message = "Logged out successfully." });
    }
}
