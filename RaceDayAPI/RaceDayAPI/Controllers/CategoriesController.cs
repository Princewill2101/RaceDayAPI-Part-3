using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // ── DELETE /api/categories/{id} ──────────────────────────
    /// <summary>
    /// Remove a category from an event. Owning Organiser only.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!SessionHelper.IsOrganiser(HttpContext.Session))
            return Forbid();

        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        // Only the Organiser who owns the event may delete its categories
        int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
        if (category.Event.OrganiserId != userId)
            return Forbid();

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
