using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Models;
using CampusPulse.Api.Helpers;

namespace CampusPulse.Api.Controllers;

// Change request: "As a student, I want to follow campus interests so that I
// can more easily discover posts and events that are relevant to me."
//
// Every action here works off the caller's own JWT-derived UserId, never a
// UserId passed in the request - a Student can only ever see/change their
// OWN followed interests, matching "A Student must not be able to ... modify
// another user's followed interests." There is deliberately no
// admin-override endpoint here: managing *which categories exist* stays
// with CategoriesController's existing Admin-only Create/Delete; this
// controller is purely the per-user follow relationship on top of that.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FollowsController(AppDbContext db)
    {
        _db = db;
    }

    // The categories the current user follows.
    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetFollowed()
    {
        var userId = User.GetUserId();

        return await _db.FollowedInterests
            .Where(f => f.UserId == userId)
            .Include(f => f.Category)
            .Select(f => f.Category)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Follow(FollowCategoryDto dto)
    {
        var userId = User.GetUserId();

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return NotFound("Category does not exist.");

        // Idempotent rather than an error - re-tapping "Follow" on something
        // you already follow should just be a no-op, not a visible failure.
        // This is what "prevent unnecessary duplicate follow records" means
        // in practice: the second call doesn't create a second row.
        var alreadyFollowing = await _db.FollowedInterests
            .AnyAsync(f => f.UserId == userId && f.CategoryId == dto.CategoryId);

        if (alreadyFollowing)
            return Ok();

        _db.FollowedInterests.Add(new FollowedInterest
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{categoryId}")]
    public async Task<ActionResult> Unfollow(int categoryId)
    {
        var userId = User.GetUserId();

        var existing = await _db.FollowedInterests
            .FirstOrDefaultAsync(f => f.UserId == userId && f.CategoryId == categoryId);

        // Already not following it - same idempotent reasoning as Follow.
        if (existing == null)
            return Ok();

        _db.FollowedInterests.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
