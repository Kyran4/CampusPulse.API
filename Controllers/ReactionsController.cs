using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Models;
using CampusPulse.Api.Helpers;

namespace CampusPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReactionsController : ControllerBase
{
    private static readonly string[] ValidTypes = { "Like", "Helpful", "Interested" };

    private readonly AppDbContext _db;

    public ReactionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{postId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Reaction>>> GetForPost(int postId)
    {
        return await _db.Reactions.Where(r => r.PostId == postId).ToListAsync();
    }

    // A user gets exactly one reaction per post (US-13: "cannot spam the
    // same reaction on a post repeatedly"). Reacting again with the same
    // type removes it (toggle off); a different type replaces it.
    [HttpPost]
    public async Task<ActionResult<Reaction>> React(ReactionCreateDto dto)
    {
        if (!ValidTypes.Contains(dto.Type))
            return BadRequest($"Type must be one of: {string.Join(", ", ValidTypes)}");

        if (!await _db.Posts.AnyAsync(p => p.PostId == dto.PostId && !p.IsHidden))
            return NotFound("Post does not exist.");

        var userId = User.GetUserId();
        var existing = await _db.Reactions
            .FirstOrDefaultAsync(r => r.PostId == dto.PostId && r.UserId == userId);

        if (existing != null)
        {
            if (existing.Type == dto.Type)
            {
                _db.Reactions.Remove(existing);
                await _db.SaveChangesAsync();
                return Ok(new { removed = true });
            }

            existing.Type = dto.Type;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        var reaction = new Reaction
        {
            PostId = dto.PostId,
            UserId = userId,
            Type = dto.Type
        };

        _db.Reactions.Add(reaction);
        await _db.SaveChangesAsync();
        return Ok(reaction);
    }
}
