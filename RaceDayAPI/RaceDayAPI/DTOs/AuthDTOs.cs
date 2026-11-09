using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

// ── Register ─────────────────────────────────────────────────
public class RegisterRequest
{
    /// <example>John Smith</example>
    [Required]
    public string FullName { get; set; } = string.Empty;

    /// <example>john@example.com</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123</example>
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <example>Organiser</example>
    [Required]
    public string Role { get; set; } = string.Empty;
}

// ── Login ────────────────────────────────────────────────────
public class LoginRequest
{
    /// <example>john@example.com</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

// ── Shared user response (no password ever leaves the API) ────
public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

// ── Profile update ────────────────────────────────────────────
public class UpdateProfileRequest
{
    /// <example>John Smith</example>
    [Required]
    public string FullName { get; set; } = string.Empty;

    /// <example>https://example.com/photo.jpg</example>
    public string? ProfilePictureUrl { get; set; }
}
